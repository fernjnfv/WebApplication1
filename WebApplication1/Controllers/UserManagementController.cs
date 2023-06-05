using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDbContext _context;
        private static List<User> users = new List<User>();

        public UsersController(UserDbContext context)
        {
            _context = context;
            // Если список пользователей пуст, добавляем пользователя Admin
            if (!users.Any(u => u.Login == "admin"))
            {
                var adminUser = new User
                {
                    Guid = Guid.NewGuid(),
                    Login = "admin",
                    Password = "admin",
                    Name = "Admin",
                    Gender = 1, // Мужчина
                    Birthday = DateTime.Parse("1990-01-01"),
                    Admin = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "admin"
                };

                users.Add(adminUser);
            }

            if (_context.Users.FirstOrDefault(u => u.Login == "admin") == null)
            {
                var adminUser = new User
                {
                    //Guid = Guid.NewGuid(),
                    Login = "admin",
                    Password = "admin",
                    Name = "Admin",
                    Gender = 1, // Мужчина
                    Birthday = DateTime.Parse("1990-01-01"),
                    Admin = true,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "admin"
                };

                _context.Users.Add(adminUser);
                _context.SaveChanges();
            }
        }


        [HttpPost("NewUser")]
        [SwaggerOperation("Create a new user")]
        public IActionResult CreateUser(bool DBorNot, [FromBody] CreateUserRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password, DBorNot))
                return Unauthorized();

            if (!isAdmin(request.Login, request.Password, DBorNot) && request.Admin)
                return Forbid();

            if(isUnicLogin(request.CreatedLogin, DBorNot))
                return BadRequest("User with the same login already exists");

            var newUser = new User
            {
                Login = request.CreatedLogin,
                Password = request.CreatedPassword,
                Name = request.Name,
                Gender = request.Gender,
                Birthday = request.Birthday,
                Admin = request.Admin,
                CreatedOn = DateTime.Now,
                CreatedBy = request.Login
            };
            if(!DBorNot)
            {
                newUser.Guid = Guid.NewGuid();
                users.Add(newUser);
            }
            else
            {
                _context.Users.Add(newUser);
                _context.SaveChanges();
            }
            

            return Ok();
        }

        [HttpPut("Update/light")]
        [SwaggerOperation("Update the name, gender, or birthday of a user")]
        public IActionResult Update(bool DBorNot,[FromBody] UpdateUserRequest request)
        {
            var currentUser = GetUserOrNull(request.Login, request.Password, DBorNot);            
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = GetUserOrNull(request.UserLogin, DBorNot);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!(currentUser.Admin || userToUpdate.RevokedOn == null))
                return BadRequest("User has been revoked and you are not admin");

            if (!(currentUser.Admin || currentUser.Guid == userToUpdate.Guid))
                return Forbid();

            userToUpdate.Name = request.Name ?? userToUpdate.Name;
            userToUpdate.Gender = request.Gender ?? userToUpdate.Gender;
            userToUpdate.Birthday = request.Birthday ?? userToUpdate.Birthday;
            userToUpdate.ModifiedOn = DateTime.Now;
            userToUpdate.ModifiedBy = currentUser.Guid.ToString();

            if (DBorNot)
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        [HttpPut("Update/password")]
        [SwaggerOperation("Update the password of a user")]
        public IActionResult UpdatePassword(bool DBorNot, [FromBody] UpdatePasswordRequest request)
        {
            var currentUser = GetUserOrNull(request.Login, request.Password, DBorNot);
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = GetUserOrNull(request.UserLogin, DBorNot);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!(currentUser.Admin || currentUser.Guid == userToUpdate.Guid))
                return Forbid();

            userToUpdate.Password = request.NewUserPassword;
            userToUpdate.ModifiedOn = DateTime.Now;
            userToUpdate.ModifiedBy = currentUser.Guid.ToString();

            if (DBorNot)
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        [HttpPut("Update/login")]
        [SwaggerOperation("Update the login of a user")]
        public IActionResult UpdateLogin(bool DBorNot, [FromBody] UpdateLoginRequest request)
        {
            var currentUser = GetUserOrNull(request.Login, request.Password, DBorNot);
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = GetUserOrNull(request.UserLogin, DBorNot);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!(currentUser.Admin || currentUser.Guid == userToUpdate.Guid))
                return Forbid();

            if(isUnicLogin(request.UserLogin, DBorNot))
                return Conflict("User with the same login already exists.");

            userToUpdate.Login = request.NewLogin;
            userToUpdate.ModifiedOn = DateTime.Now;
            userToUpdate.ModifiedBy = currentUser.Guid.ToString();

            if (DBorNot)
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        

        [HttpGet("active")]
        [SwaggerOperation("Get a list of all active users")]
        public IActionResult GetActiveUsers(bool DBorNot, [FromQuery] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password, DBorNot))
                return Forbid();

            var activeUsers = GetActiveUsersList(DBorNot);

            return Ok(activeUsers);
        }

        [HttpGet("infoByLogin")]
        [SwaggerOperation("Get a user by login")]
        public IActionResult GetUserByLogin(bool DBorNot, [FromQuery] Credentials credentials, [FromQuery] string userLogin)
        {
            if (!isAdmin(credentials.Login, credentials.Password, DBorNot))
                return Forbid();

            var choisedUser = GetUserOrNull(userLogin, DBorNot);
            if (choisedUser == null)
                return NotFound();

            var response = new
            {
                Name = choisedUser.Name,
                Gender = choisedUser.Gender,
                Birthday = choisedUser.Birthday,
                IsActive = choisedUser.RevokedOn == null
            };

            return Ok(response);
        }

        [HttpGet("infoForUser")]
        [SwaggerOperation("Get a user by login and password")]
        public IActionResult GetUserByLoginAndPassword(bool DBorNot, [FromQuery] Credentials credentials, [FromQuery] Credentials request)
        {
            var currentUser = GetUserOrNull(credentials.Login, credentials.Password, DBorNot);
            if (currentUser == null)
                return Unauthorized();


            if (!((request.Login == credentials.Login) && (request.Password == credentials.Password)))
                return NotFound("is it not your login or password");

            if ((currentUser.RevokedOn == null))
                return BadRequest("You have been revoked");

            var response = new
            {
                Name = currentUser.Name,
                Gender = currentUser.Gender,
                Birthday = currentUser.Birthday,
                IsActive = currentUser.RevokedOn == null
            };

            return Ok(currentUser);
        }

        [HttpGet("upperAge")]
        [SwaggerOperation("Get all users older than a specified age")]
        public IActionResult GetUsersOlderThanAge(bool DBorNot, [FromQuery]int age, [FromQuery] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password, DBorNot))
                return Forbid();

            var usersOlderThanAge = GetUsersOlderThanAgeList(age, DBorNot);

            return Ok(usersOlderThanAge);
        }

        [HttpDelete("deleteUser")]
        [SwaggerOperation("Delete a user by login (soft or hard delete)")]
        public IActionResult DeleteUser(bool DBorNot, [FromQuery] string userLogin, [FromBody] DeleteUserRequest request)
        {

            var currentUser = GetAdminOrNull(request.Login, request.Password, DBorNot);
            if (currentUser == null)
                return Unauthorized();

            var user = GetUserOrNull(userLogin, DBorNot);

            if (user == null)
                return NotFound();

            if (request.SoftDelete)
            {
                user.RevokedOn = DateTime.Now;
                user.RevokedBy = currentUser.Guid.ToString();
            }
            else
            {
                removeUser(user, DBorNot);
            }

            if (DBorNot)
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        [HttpPut("restoreUser")]
        [SwaggerOperation("Restore a user")]
        public IActionResult RestoreUser(bool DBorNot, [FromQuery]string login, [FromBody] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password, DBorNot))
                return Forbid();

            var user = GetUserOrNull(login, DBorNot);

            if (user == null)
                return NotFound();


            user.RevokedOn = null;
            user.RevokedBy = null;

            if (DBorNot)
            {
                _context.SaveChanges();
            }

            return Ok();
        }

        private void removeUser(User user, bool DBorNot)
        {
            if (!DBorNot)
            {
                users.Remove(user);
            }
            else
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }
        private bool IsAuthorized(string login, string password, bool DBorNot)
        {
            if (!DBorNot)
            {
                var user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
                return user != null;
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
                return user != null;
            }      
        }

        private User? GetUserOrNull(string login, string password, bool DBorNot)
        {

            if (!DBorNot)
            {
                var user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
                return user;
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
                return user;
            }
        }

        private User? GetUserOrNull(string login, bool DBorNot)
        {
            if (!DBorNot)
            {
                var user = users.FirstOrDefault(u => u.Login == login);
                return user;
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login);
                return user;
            }
        }

        private bool isUnicLogin(string login, bool DBorNot)
        {
            if (!DBorNot)
            {
                return users.Any(u => u.Login == login);
            }
            else
            {
                return _context.Users.Any(u => u.Login == login);
            }
        }

        private bool isAdmin(string login, string password, bool DBorNot)
        {
            if (!DBorNot)
            {
                var user = users.FirstOrDefault(u => u.Login == login && u.Password == password && u.Admin);
                return user != null;
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login && u.Password == password && u.Admin);
                return user != null;
            }
        }

        private User? GetAdminOrNull(string login, string password, bool DBorNot)
        {
            if (!DBorNot)
            {
                var user = users.FirstOrDefault(u => u.Login == login && u.Password == password && u.Admin);
                return user;
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login && u.Password == password && u.Admin);
                return user;
            }
        }

        public class ActiveUserResponse
        {
            public Guid Guid { get; set; }
            public string Login { get; set; }
            public string Name { get; set; }

            public ActiveUserResponse(Guid guid, string login, string name)
            {
                Guid = guid;
                Login = login;
                Name = name;
            }
        }

        private List<ActiveUserResponse> GetActiveUsersList(bool DBorNot)
        {
            if (!DBorNot)
                return users.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).Select(u => new ActiveUserResponse
                (
                    u.Guid,
                    u.Login,
                    u.Name
                )).ToList();
            else
                return _context.Users.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).Select(u => new ActiveUserResponse
                (
                    u.Guid,
                    u.Login,
                    u.Name
                )).ToList();
        }

        private List<ActiveUserResponse> GetUsersOlderThanAgeList(int age, bool DBorNot)
        {
            if (!DBorNot)
                return users.Where(u => u.Birthday.HasValue && (DateTime.Now - u.Birthday.Value).TotalDays > age * 365).Select(u => new ActiveUserResponse
                (
                    u.Guid,
                    u.Login,
                    u.Name
                )).ToList();
            else
                return _context.Users.Where(u => u.Birthday.HasValue && (DateTime.Now - u.Birthday.Value).TotalDays > age * 365).Select(u => new ActiveUserResponse
                (
                    u.Guid,
                    u.Login,
                    u.Name
                )).ToList();
        }
    }
}
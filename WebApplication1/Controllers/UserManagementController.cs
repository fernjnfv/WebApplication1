using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static List<User> users = new List<User>();

        public UsersController()
        {
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
        }

        private const string AdminLogin = "admin";
        private const string AdminPassword = "admin";

        [HttpPost("NewUser")]
        [SwaggerOperation("Create a new user")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();

            if (!isAdmin(request.Login, request.Password) && request.Admin)
                return Forbid();

            if (users.Any(u => u.Login == request.CreatedLogin))
                return BadRequest("User with the same login already exists");

            var newUser = new User
            {
                Guid = Guid.NewGuid(),
                Login = request.CreatedLogin,
                Password = request.CreatedPassword,
                Name = request.Name,
                Gender = request.Gender,
                Birthday = request.Birthday,
                Admin = request.Admin,
                CreatedOn = DateTime.Now,
                CreatedBy = request.Login
            };

            users.Add(newUser);

            return Ok();
        }

        [HttpPut("Update/light")]
        [SwaggerOperation("Update the name, gender, or birthday of a user")]
        public IActionResult Update([FromBody] UpdateUserRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();
            var currentUser = users.FirstOrDefault(u => u.Login == request.Login);            
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = users.FirstOrDefault(u => u.Login == request.UserLogin);
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

            return Ok();
        }

        [HttpPut("Update/password")]
        [SwaggerOperation("Update the password of a user")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var currentUser = users.FirstOrDefault(u => u.Login == request.Login && u.Password == request.Password);
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = users.FirstOrDefault(u => u.Login == request.UserLogin);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!(currentUser.Admin || currentUser.Guid == userToUpdate.Guid))
                return Forbid();

            userToUpdate.Password = request.NewUserPassword;
            userToUpdate.ModifiedOn = DateTime.Now;
            userToUpdate.ModifiedBy = currentUser.Guid.ToString();

            return Ok();
        }

        [HttpPut("Update/login")]
        [SwaggerOperation("Update the login of a user")]
        public IActionResult UpdateLogin([FromBody] UpdateLoginRequest request)
        {
            var currentUser = users.FirstOrDefault(u => u.Login == request.Login && u.Password == request.Password);
            if (currentUser == null)
                return Unauthorized();

            var userToUpdate = users.FirstOrDefault(u => u.Login == request.UserLogin);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!(currentUser.Admin || currentUser.Guid == userToUpdate.Guid))
                return Forbid();

            if (users.Any(u => u.Login == request.NewLogin))
                return Conflict("User with the same login already exists.");

            userToUpdate.Login = request.NewLogin;
            userToUpdate.ModifiedOn = DateTime.Now;
            userToUpdate.ModifiedBy = currentUser.Guid.ToString();

            return Ok();
        }

        [HttpGet("active")]
        [SwaggerOperation("Get a list of all active users")]
        public IActionResult GetActiveUsers([FromQuery] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password))
                return Forbid();

            var activeUsers = users.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).Select(u => new
            {
                u.Guid,
                u.Login,
                u.Name
            }).ToList();

            return Ok(activeUsers);
        }

        [HttpGet("infoByLogin")]
        [SwaggerOperation("Get a user by login")]
        public IActionResult GetUserByLogin([FromQuery] Credentials credentials, [FromQuery] string userLogin)
        {
            if (!isAdmin(credentials.Login, credentials.Password))
                return Forbid();

            var user = users.FirstOrDefault(u => u.Login == userLogin);
            if (user == null)
                return NotFound();

            var response = new
            {
                Name = user.Name,
                Gender = user.Gender,
                Birthday = user.Birthday,
                IsActive = user.RevokedOn == null
            };

            return Ok(response);
        }

        [HttpGet("infoForUser")]
        [SwaggerOperation("Get a user by login and password")]
        public IActionResult GetUserByLoginAndPassword([FromQuery] Credentials credentials, [FromQuery] Credentials request)
        {
            var currentUser = users.FirstOrDefault(u => u.Login == credentials.Login && u.Password == credentials.Password);
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
        public IActionResult GetUsersOlderThanAge([FromQuery]int age, [FromQuery] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password))
                return Forbid();

            var usersOlderThanAge = users.Where(u => u.Birthday.HasValue && (DateTime.Now - u.Birthday.Value).TotalDays > age * 365).Select(u => new
            {
                u.Guid,
                u.Login,
                u.Name
            }).ToList();

            return Ok(usersOlderThanAge);
        }

        [HttpDelete("deleteUser")]
        [SwaggerOperation("Delete a user by login (soft or hard delete)")]
        public IActionResult DeleteUser([FromQuery] string userLogin, [FromBody] DeleteUserRequest request)
        {
            if (!isAdmin(request.Login, request.Password))
                return Forbid();

            var currentUser = users.FirstOrDefault(u => u.Login == request.Login);
            if (currentUser == null)
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == userLogin);

            if (user == null)
                return NotFound();

            if (request.SoftDelete)
            {
                user.RevokedOn = DateTime.Now;
                user.RevokedBy = currentUser.Guid.ToString();
            }
            else
            {
                users.Remove(user);
            }

            return Ok();
        }

        [HttpPut("restoreUser")]
        [SwaggerOperation("Restore a user")]
        public IActionResult RestoreUser([FromQuery]string login, [FromBody] Credentials credentials)
        {
            if (!isAdmin(credentials.Login, credentials.Password))
                return Forbid();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();


            user.RevokedOn = null;
            user.RevokedBy = null;

            return Ok();
        }

        private bool IsAuthorized(string login, string password)
        {
            var user = users.FirstOrDefault(u => u.Login == login && u.Password == password);
            return user != null;
        }

        private bool isAdmin(string login, string password)
        {
            var user = users.FirstOrDefault(u => u.Login == login && u.Password == password && u.Admin);
            return user != null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
            // Добавление пользователя admin при создании контроллера
            var adminUser = new User
            {
                Guid = Guid.NewGuid(),
                Login = "admin",
                Password = "admin",
                Name = "Admin",
                Gender = 1,
                Birthday = null,
                Admin = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "system"
            };

            users.Add(adminUser);
        }

        private const string AdminLogin = "admin";
        private const string AdminPassword = "admin";

        [HttpPost]
        [SwaggerOperation("Create a new user")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request, [FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            if (!isAdmin(credentials.Login, credentials.Password))
                return Forbid();

            if (users.Any(u => u.Login == request.Login))
                return BadRequest("User with the same login already exists");

            var newUser = new User
            {
                Guid = Guid.NewGuid(),
                Login = request.Login,
                Password = request.Password,
                Name = request.Name,
                Gender = request.Gender,
                Birthday = request.Birthday,
                Admin = request.Admin,
                CreatedOn = DateTime.Now,
                CreatedBy = credentials.Login
            };

            users.Add(newUser);

            return Ok();
        }

        [HttpPut("{login}")]
        [SwaggerOperation("Update the name, gender, or birthday of a user")]
        public IActionResult Update(string login, [FromBody] UpdateUserRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            //if (user.Login != GetCurrentUserLogin() && !isAdmin())
            //    return Forbid();

            user.Name = request.Name ?? user.Name;
            user.Gender = request.Gender ?? user.Gender;
            user.Birthday = request.Birthday ?? user.Birthday;
            user.ModifiedOn = DateTime.Now;
            user.ModifiedBy = GetCurrentUserLogin();

            return Ok();
        }

        [HttpPut("{login}/password")]
        [SwaggerOperation("Update the password of a user")]
        public IActionResult UpdatePassword(string login, [FromBody] UpdatePasswordRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            //if (user.Login != GetCurrentUserLogin() && !IsAdmin())
            //    return Forbid();

            user.Password = request.NewPassword;
            user.ModifiedOn = DateTime.Now;
            user.ModifiedBy = GetCurrentUserLogin();

            return Ok();
        }

        [HttpPut("{login}/login")]
        [SwaggerOperation("Update the login of a user")]
        public IActionResult UpdateLogin(string login, [FromBody] UpdateLoginRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            //if (user.Login != GetCurrentUserLogin() && !IsAdmin())
            //    return Forbid();

            if (users.Any(u => u.Login == request.NewLogin))
                return Conflict("User with the same login already exists.");

            user.Login = request.NewLogin;
            user.ModifiedOn = DateTime.Now;
            user.ModifiedBy = GetCurrentUserLogin();

            return Ok();
        }

        [HttpGet("active")]
        [SwaggerOperation("Get a list of all active users")]
        public IActionResult GetActiveUsers([FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            var activeUsers = users.Where(u => u.RevokedOn == null).ToList();
            return Ok(activeUsers);
        }

        [HttpGet("{login}")]
        [SwaggerOperation("Get a user by login")]
        public IActionResult GetUserByLogin(string login, [FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("{login}/password")]
        [SwaggerOperation("Get a user by login and password")]
        public IActionResult GetUserByLoginAndPassword(string login, [FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login && u.Password == credentials.Password);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("age/{age}")]
        [SwaggerOperation("Get all users older than a specified age")]
        public IActionResult GetUsersOlderThanAge(int age, [FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            var usersOlderThanAge = users.Where(u => u.Birthday.HasValue && (DateTime.Now - u.Birthday.Value).TotalDays > age * 365).ToList();
            return Ok(usersOlderThanAge);
        }

        [HttpDelete("{login}")]
        [SwaggerOperation("Delete a user by login (soft or hard delete)")]
        public IActionResult DeleteUser(string login, [FromQuery] DeleteUserRequest request)
        {
            if (!IsAuthorized(request.Login, request.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            //if (!IsAdmin())
            //    return Forbid();

            if (request.SoftDelete)
            {
                user.RevokedOn = DateTime.Now;
                user.RevokedBy = GetCurrentUserLogin();
            }
            else
            {
                users.Remove(user);
            }

            return Ok();
        }

        [HttpPut("{login}/restore")]
        [SwaggerOperation("Restore a user")]
        public IActionResult RestoreUser(string login, [FromQuery] Credentials credentials)
        {
            if (!IsAuthorized(credentials.Login, credentials.Password))
                return Unauthorized();

            var user = users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return NotFound();

            //if (!IsAdmin())
            //    return Forbid();

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

        private string GetCurrentUserLogin()
        {
            // Implement your logic to retrieve the current user's login from the request context.
            // For the sake of simplicity, we'll use a hardcoded value here.
            return "admin";
        }
    }

    public class User
    {
        public Guid Guid { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public bool Admin { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string RevokedBy { get; set; }
    }

    public class CreateUserRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public bool Admin { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public int? Gender { get; set; }
        public DateTime? Birthday { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }

    public class UpdateLoginRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string NewLogin { get; set; }
    }

    public class Credentials
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class DeleteUserRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public bool SoftDelete { get; set; }
    }
}
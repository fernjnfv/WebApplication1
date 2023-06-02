using System.ComponentModel.DataAnnotations;

namespace UserManagement
{
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
        public string? ModifiedBy { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? RevokedBy { get; set; }
    }



    public class Credentials
    {
        [Required]
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class DeleteUserRequest : Credentials
    {
        public bool SoftDelete { get; set; }
    }

    public class CreateUserRequest : Credentials
    {
        [Required]
        public string CreatedLogin { get; set; }
        [Required]
        public string CreatedPassword { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [Range(0, 1, ErrorMessage = "Gender must be either 0 or 1")]
        public int Gender { get; set; }
        public DateTime? Birthday { get; set; }
        [Required]
        public bool Admin { get; set; }
    }

    public class UpdateUserRequest : Credentials
    {
        [Required]
        public string UserLogin { get; set; }
        public string? Name { get; set; }
        [Range(0, 1, ErrorMessage = "Gender must be either 0 or 1")]
        public int? Gender { get; set; }
        public DateTime? Birthday { get; set; }
    }

    public class UpdatePasswordRequest : Credentials
    {
        [Required]
        public string UserLogin { get; set; }
        [Required]
        public string NewUserPassword { get; set; }
    }

    public class UpdateLoginRequest : Credentials
    {
        [Required]
        public string UserLogin { get; set; }
        [Required]
        public string NewLogin { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class RegisterModel
    {
        [Required]
        [StringLength(51, ErrorMessage = "The username cannot exceed 50 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])[a-zA-Z0-9_]+$", ErrorMessage = "Username must contain at least one letter and can only include letters, numbers, and underscores.")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

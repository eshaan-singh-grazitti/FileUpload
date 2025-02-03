using System.ComponentModel.DataAnnotations;

namespace FileUpload.Models
{
    public class RegisterModel
    {
        [Required]
        [StringLength(51, ErrorMessage = "The username cannot exceed 50 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])[a-zA-Z0-9_]+$", ErrorMessage = "Username must contain at least one letter and can only include letters, numbers, and underscores.")]
        public string UserName { get; set; } = null!;

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email address format.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}

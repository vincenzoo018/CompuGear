using System.ComponentModel.DataAnnotations;

namespace CompuGear.Models.ViewModels
{
	public class ForgotPasswordViewModel
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		[StringLength(100)]
		[Display(Name = "Email Address")]
		public string Email { get; set; } = string.Empty;
	}

	public class VerifyOtpViewModel
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		[StringLength(100)]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "OTP is required")]
		[RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits")]
		[Display(Name = "OTP Code")]
		public string OtpCode { get; set; } = string.Empty;
	}

	public class ResetPasswordViewModel
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		[StringLength(100)]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "New password is required")]
		[DataType(DataType.Password)]
		[StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
		public string NewPassword { get; set; } = string.Empty;

		[Required(ErrorMessage = "Confirm password is required")]
		[DataType(DataType.Password)]
		[Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}
}

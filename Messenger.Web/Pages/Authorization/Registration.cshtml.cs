using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Messenger.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Messenger.Web.Pages.Authorization
{
    public class RegistrationModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Логин не может быть пустым")]
        public string Email { get; set; } = string.Empty;
        
        [BindProperty]
        [Required(ErrorMessage = "Пароль не может быть пустым")]
        public string Password { get; set; } = string.Empty;
        
        [BindProperty]
        [Required(ErrorMessage = "Имя не может быть пустым")]
        public string FirstName { get; set; } = string.Empty;
        
        [BindProperty]
        public string MiddleName { get; set; } = string.Empty;

        [BindProperty] 
        [Required(ErrorMessage = "Фамилия не может быть пустым")] 
        public string LastName { get; set; } = string.Empty;
        
        [BindProperty]
        [Required(ErrorMessage = "Дата рождения не может быть пустым")]
        public DateTime Birthday { get; set; }
        
        [BindProperty]
        [Required(ErrorMessage = "Телефон не может быть пустым")]
        public string Phone { get; set; } = string.Empty;
        
        [BindProperty]
        public string? ErrorMessage { get; private set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var registerRequest = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                FirstName = FirstName,
                MiddleName = MiddleName,
                LastName = LastName,
                Phone = Phone,
                BirthDate = Birthday
            };

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var content = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync("https://localhost:7001/api/Authorization/register", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        ErrorMessage = "Регистрация не прошла";
                        return Page();
                    }

                    return RedirectToPage("/Authorization/Authorization");
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Ошибка соединения с API: " + ex.Message;
                    return Page();
                }
            }
        }
    }
}

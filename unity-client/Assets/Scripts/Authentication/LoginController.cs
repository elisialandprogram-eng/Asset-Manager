using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using EternalKingdoms.Core;
using EternalKingdoms.Managers;
using EternalKingdoms.UI;

namespace EternalKingdoms.Authentication
{
    /// <summary>
    /// Drives the Login scene UI.
    ///
    /// Wired to:
    ///   - EmailField (TMP_InputField)
    ///   - PasswordField (TMP_InputField)
    ///   - RememberMeToggle (Toggle)
    ///   - LoginButton (Button)
    ///   - ErrorLabel (TextMeshProUGUI)
    ///   - LoadingOverlay (GameObject)
    ///
    /// Calls AuthManager.Login() and navigates to World scene on success.
    /// </summary>
    public class LoginController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailField;
        [SerializeField] private TMP_InputField passwordField;
        [SerializeField] private Toggle rememberMeToggle;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI errorLabel;
        [SerializeField] private GameObject loadingOverlay;

        private bool _isSubmitting;

        private void Start()
        {
            // Pre-fill email if remembered
            bool remembered = SaveManager.Instance.GetBool(SaveManager.KEY_REMEMBER_ME);
            if (remembered)
            {
                emailField.text = SaveManager.Instance.GetString(SaveManager.KEY_USER_EMAIL);
                rememberMeToggle.isOn = true;
            }

            ClearError();
            loginButton.onClick.AddListener(OnLoginClicked);

            // Allow Enter key to submit
            passwordField.onSubmit.AddListener(_ => OnLoginClicked());
        }

        private void OnLoginClicked()
        {
            if (_isSubmitting) return;
            StartCoroutine(HandleLogin());
        }

        private IEnumerator HandleLogin()
        {
            string email = emailField.text.Trim();
            string password = passwordField.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your email and password.");
                yield break;
            }

            _isSubmitting = true;
            SetLoading(true);
            ClearError();

            yield return AuthManager.Instance.Login(
                email, password,
                onSuccess: (user) =>
                {
                    SaveManager.Instance.SetBool(SaveManager.KEY_REMEMBER_ME, rememberMeToggle.isOn);
                    if (!rememberMeToggle.isOn)
                        SaveManager.Instance.DeleteKey(SaveManager.KEY_USER_EMAIL);

                    Debug.Log($"[LoginController] Login succeeded — routing to World.");
                    SceneController.Instance.GoToWorld();
                },
                onError: (msg) =>
                {
                    SetLoading(false);
                    _isSubmitting = false;
                    ShowError(string.IsNullOrEmpty(msg) ? "Login failed. Please try again." : msg);
                }
            );
        }

        private void SetLoading(bool loading)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(loading);
            loginButton.interactable = !loading;
            emailField.interactable = !loading;
            passwordField.interactable = !loading;
        }

        private void ShowError(string message)
        {
            if (errorLabel != null)
            {
                errorLabel.text = message;
                errorLabel.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            if (errorLabel != null)
            {
                errorLabel.text = "";
                errorLabel.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            loginButton.onClick.RemoveListener(OnLoginClicked);
        }
    }
}

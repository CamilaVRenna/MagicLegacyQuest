using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;       

public class MainMenuController : MonoBehaviour
{
    public string gameScene = "TiendaDeMagia"; 
    public GameObject helpPanel;
    public Button nextButton;
    public Button newGameButton;
    public GameObject confirmNewGamePanel;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        bool saveExists = PlayerPrefs.HasKey("SaveExists") && PlayerPrefs.GetInt("SaveExists") == 1;
        if (nextButton != null) nextButton.interactable = saveExists;
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
    }

    public void OnPlayPressed()
    {
        if (!string.IsNullOrEmpty(gameScene))
        {
            GestorJuego.CargarEscenaConPantallaDeCarga(gameScene);
        }
        else
        {
            Debug.LogError("Game scene name not specified in MainMenuController!");
        }
    }

    public void OnHelpPressed()
    {
        Debug.Log("Showing Help Panel...");
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Help Panel not assigned in MainMenuController!");
        }
    }

    public void OnExitPressed()
    {
        Debug.Log("Exiting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnHelpClosed()
    {
        Debug.Log("Closing Help Panel...");
        if (helpPanel != null)
        {
            helpPanel.SetActive(false); 
        }
    }

    public void OnNextPressed()
    {
        GestorJuego.CargarEscenaConPantallaDeCarga(gameScene);
    }

    public void OnNewGamePressed()
    {
        bool saveExists = PlayerPrefs.HasKey("SaveExists") && PlayerPrefs.GetInt("SaveExists") == 1;
        if (saveExists && confirmNewGamePanel != null)
        {
            confirmNewGamePanel.SetActive(true);
        }
        else
        {
            ConfirmNewGame();
        }
    }

    public void ConfirmNewGame()
    {
        Debug.Log("Deleting data for New Game...");
        PlayerPrefs.DeleteKey("SaveExists");
        PlayerPrefs.DeleteKey("CurrentDay");
        PlayerPrefs.DeleteKey("CurrentMoney");
        PlayerPrefs.DeleteKey("CurrentHour");
        PlayerPrefs.DeleteKey("IngredientsStock");
        /* Delete other keys */
        PlayerPrefs.Save();
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
        GestorJuego.CargarEscenaConPantallaDeCarga(gameScene);
    }

    public void CancelNewGame()
    {
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
    }
}
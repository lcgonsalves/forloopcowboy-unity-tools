using System;
using LobbyRelaySample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace forloopcowboy_unity_tools.Scripts.GUI.Docs
{
    public class MainMenuUIController : MonoBehaviour
    {
        public GameObject lobbyUI;
        
        private UIDocument document;
        
        private VisualElement mainMenu;
        private Button multiplayerButton;
        private Button quitButton;
        
        private VisualElement multiplayerMenu;
        private Button hostButton;
        private Button joinButton;
        private Button returnButton;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            
            VisualElement root = document.rootVisualElement;

            mainMenu = root.Q<VisualElement>("main-menu");
            multiplayerButton = root.Q<Button>("multiplayer-btn");
            quitButton = root.Q<Button>("quit-btn");
            
            multiplayerMenu = root.Q<VisualElement>("multiplayer-menu");
            hostButton = root.Q<Button>("host-btn");
            joinButton = root.Q<Button>("join-btn");
            returnButton = root.Q<Button>("return-to-main-menu-btn");

            multiplayerButton.clicked += OpenMultiplayerMenu;
            returnButton.clicked += ReturnToMainMenu;

            hostButton.clicked += HostSession;
            joinButton.clicked += JoinSession;
            
            quitButton.clicked += Application.Quit;
        }

        private void OpenMultiplayerMenu()
        {
            mainMenu.style.display = DisplayStyle.None;
            // multiplayerMenu.style.display = DisplayStyle.Flex;
            
            lobbyUI.SetActive(true);
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
        }

        private void ReturnToMainMenu()
        {
            mainMenu.style.display = DisplayStyle.Flex;
            // multiplayerMenu.style.display = DisplayStyle.None;
            
            lobbyUI.SetActive(false);
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.Menu);
        }

        private void HostSession()
        {
            NetworkManager.Singleton.StartHost();
            document.enabled = false;
        }
        
        private void JoinSession()
        {
            NetworkManager.Singleton.StartClient();
            document.enabled = false;
        }
    }
}
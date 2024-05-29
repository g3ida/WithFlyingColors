// using Godot;
// using System;

// public class MainMenu : GameMenu
// {
//     private Label currentSlotLabelNode;
//     private MenuBox menuBoxNode;
//     private ResetDialog resetSlotDialogNode;

//     public override void _Ready()
//     {
//         menuBoxNode = GetNode<MenuBox>("MenuBox");
//         resetSlotDialogNode = GetNode<ResetDialog>("ResetDialogContainer");
//         currentSlotLabelNode = GetNode<Label>("CurrentSlotLabel");

//         SaveGame.Init();

//         currentSlotLabelNode.Text = $"Current slot: {SaveGame.currentSlotIndex + 1}";
//     }

//     public void ShowResetDataDialog()
//     {
//         resetSlotDialogNode.ShowDialog();
//     }

//     public override bool OnMenuButtonPressed(MenuButtons menuButton)
//     {
//         switch (menuButton)
//         {
//             case MenuButtons.QUIT:
//                 if (screenState == MenuScreenState.ENTERED)
//                 {
//                     GetTree().Quit();
//                 }
//                 return true;
//             case MenuButtons.PLAY:
//                 return true;
//             case MenuButtons.STATS:
//                 NavigateToScreen(MenuManager.Menus.STATS_MENU);
//                 return true;
//             case MenuButtons.SETTINGS:
//                 NavigateToScreen(MenuManager.Menus.SETTINGS_MENU);
//                 return true;
//             case MenuButtons.BACK:
//                 menuBoxNode.HideSubMenuIfNeeded();
//                 return true;
//             default:
//                 return ProcessPlaySubMenus(menuButton);
//         }
//     }

//     private bool ProcessPlaySubMenus(MenuButtons menuButton)
//     {
//         switch (menuButton)
//         {
//             case MenuButtons.NEW_GAME:
//                 if (SaveGame.DoesSlotHaveProgress(SaveGame.currentSlotIndex))
//                 {
//                     resetSlotDialogNode.ShowDialog();
//                 }
//                 else
//                 {
//                     NavigateToScreen(MenuManager.Menus.GAME);
//                     menuBoxNode.HideSubMenuIfNeeded();
//                 }
//                 return true;
//             case MenuButtons.CONTINUE_GAME:
//                 menuBoxNode.HideSubMenuIfNeeded();
//                 NavigateToScreen(MenuManager.Menus.GAME);
//                 return true;
//             case MenuButtons.SELECT_SLOT:
//                 menuBoxNode.HideSubMenuIfNeeded();
//                 NavigateToScreen(MenuManager.Menus.SELECT_SLOT);
//                 return true;
//             default:
//                 return false;
//         }
//     }

//     private void OnResetSlotDialogConfirmed()
//     {
//         SaveGame.RemoveSaveSlot(SaveGame.currentSlotIndex);
//         menuBoxNode.HideSubMenuIfNeeded();
//         NavigateToScreen(MenuManager.Menus.GAME);
//     }
// }

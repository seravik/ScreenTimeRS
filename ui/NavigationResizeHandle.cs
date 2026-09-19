using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace ScreenTimeRS.UI;

/// <summary>Transparent hit target used to resize the NavigationView pane.</summary>
public sealed class NavigationResizeHandle : Grid
{
    public NavigationResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    public void SetResizeCursor()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}

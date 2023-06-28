// code based on TestPlugin of the original ILSpy repository

using ICSharpCode.ILSpy;

namespace BatchExportPlugin
{
    // Menu: menu into which the item is added
    // MenuIcon: optional, icon to use for the menu item. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
    // Header: text on the menu item
    // MenuCategory: optional, used for grouping related menu items together. A separator is added between different groups.
    // MenuOrder: controls the order in which the items appear (items are sorted by this value)
    [ExportMainMenuCommand(ParentMenuID = "_File", MenuIcon = "export.png", Header = "_Batch Export", MenuCategory = "Open", MenuOrder = 2.5)]
    // ToolTip: the tool tip
    // ToolbarIcon: The icon. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
    // ToolbarCategory: optional, used for grouping related toolbar items together. A separator is added between different groups.
    // ToolbarOrder: controls the order in which the items appear (items are sorted by this value)
    [ExportToolbarCommand(ToolTip = "exports multiple desired assemblies at once", ToolbarIcon = "export.png", ToolbarCategory = "View", ToolbarOrder = 10 )]
    public class BatchExportCommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            WindowExport batchExport = new WindowExport();
            batchExport.ShowDialog();
        }
    }
}

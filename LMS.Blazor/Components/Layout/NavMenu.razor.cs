using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace LMS.Blazor.Components.Layout
{
    public partial class NavMenu
    {
        private string? currentUrl;
        protected override void OnInitialized()
        {
            currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            NavigationManager.LocationChanged += OnLocationChanged;
        }
        private void OnLocationChanged(object? s, LocationChangedEventArgs e)
        {
            currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
            StateHasChanged();
        }
        public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
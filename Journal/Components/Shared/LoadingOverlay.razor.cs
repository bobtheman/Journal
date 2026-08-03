namespace Journal.Components.Shared
{
    public partial class LoadingOverlay
    {
        private bool _isLoading;

        protected override void OnInitialized()
        {
            _isLoading = LoadingService.IsLoading;
            LoadingService.Changed += OnLoadingChanged;
        }

        private void OnLoadingChanged()
        {
            InvokeAsync(() =>
            {
                _isLoading = LoadingService.IsLoading;
                StateHasChanged();
            });
        }

        public void Dispose()
        {
            LoadingService.Changed -= OnLoadingChanged;
        }
    }
}
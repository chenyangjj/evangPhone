namespace EvangPL.Components
{
    public static class ScanHelper
    {
        /// <summary>
        /// スキャンページを開き、結果をコールバックで返す
        /// </summary>
        /// <param name="navigation">呼び出し元の Navigation</param>
        /// <param name="onResult">結果を受け取るコールバック</param>
        public static async Task ScanAsync(INavigation navigation, Action<string> onResult)
        {
            if (navigation.NavigationStack.LastOrDefault() is ScanPage) return;

            var scanPage = new ScanPage();
            scanPage.BarcodeScanned += (s, code) => onResult(code);
            await navigation.PushAsync(scanPage);
        }
    }
}
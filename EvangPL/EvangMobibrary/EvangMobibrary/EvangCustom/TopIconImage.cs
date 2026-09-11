namespace EvangSol.Mobibrary.EvangCustom
{
    public class TopIconImage : Image
    {
        public TopIconImage()
        {
#if ANDROID
            HeightRequest = 64;
            WidthRequest = 64;
#elif WINDOWS
            HeightRequest = 48;
            WidthRequest = 48;
#endif
        }
    }
}

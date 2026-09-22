package crc64f1ffb0d0a422aad0;


public class OnKeyBoardDrawableTouchListenerNoKeyboard
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.View.OnTouchListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTouch:(Landroid/view/View;Landroid/view/MotionEvent;)Z:GetOnTouch_Landroid_view_View_Landroid_view_MotionEvent_Handler:Android.Views.View/IOnTouchListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("EvangSol.Mobibrary.Platforms.Android.OnKeyBoardDrawableTouchListenerNoKeyboard, EvangMobibrary", OnKeyBoardDrawableTouchListenerNoKeyboard.class, __md_methods);
	}

	public OnKeyBoardDrawableTouchListenerNoKeyboard ()
	{
		super ();
		if (getClass () == OnKeyBoardDrawableTouchListenerNoKeyboard.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.OnKeyBoardDrawableTouchListenerNoKeyboard, EvangMobibrary", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onTouch (android.view.View p0, android.view.MotionEvent p1)
	{
		return n_onTouch (p0, p1);
	}

	private native boolean n_onTouch (android.view.View p0, android.view.MotionEvent p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}

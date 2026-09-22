package crc64f1ffb0d0a422aad0;


public class MauiAutoComplete
	extends android.widget.AutoCompleteTextView
	implements
		mono.android.IGCUserPeer,
		android.text.TextWatcher,
		android.text.NoCopySpan
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreateDrawableState:(I)[I:GetOnCreateDrawableState_IHandler\n" +
			"n_dispatchKeyEvent:(Landroid/view/KeyEvent;)Z:GetDispatchKeyEvent_Landroid_view_KeyEvent_Handler\n" +
			"n_afterTextChanged:(Landroid/text/Editable;)V:GetAfterTextChanged_Landroid_text_Editable_Handler:Android.Text.ITextWatcherInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_beforeTextChanged:(Ljava/lang/CharSequence;III)V:GetBeforeTextChanged_Ljava_lang_CharSequence_IIIHandler:Android.Text.ITextWatcherInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onTextChanged:(Ljava/lang/CharSequence;III)V:GetOnTextChanged_Ljava_lang_CharSequence_IIIHandler:Android.Text.ITextWatcherInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", MauiAutoComplete.class, __md_methods);
	}

	public MauiAutoComplete (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3, android.content.res.Resources.Theme p4)
	{
		super (p0, p1, p2, p3, p4);
		if (getClass () == MauiAutoComplete.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib:Android.Content.Res.Resources+Theme, Mono.Android", this, new java.lang.Object[] { p0, p1, p2, p3, p4 });
		}
	}

	public MauiAutoComplete (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == MauiAutoComplete.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public MauiAutoComplete (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == MauiAutoComplete.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public MauiAutoComplete (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == MauiAutoComplete.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public MauiAutoComplete (android.content.Context p0)
	{
		super (p0);
		if (getClass () == MauiAutoComplete.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiAutoComplete, EvangMobibrary", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public int[] onCreateDrawableState (int p0)
	{
		return n_onCreateDrawableState (p0);
	}

	private native int[] n_onCreateDrawableState (int p0);

	public boolean dispatchKeyEvent (android.view.KeyEvent p0)
	{
		return n_dispatchKeyEvent (p0);
	}

	private native boolean n_dispatchKeyEvent (android.view.KeyEvent p0);

	public void afterTextChanged (android.text.Editable p0)
	{
		n_afterTextChanged (p0);
	}

	private native void n_afterTextChanged (android.text.Editable p0);

	public void beforeTextChanged (java.lang.CharSequence p0, int p1, int p2, int p3)
	{
		n_beforeTextChanged (p0, p1, p2, p3);
	}

	private native void n_beforeTextChanged (java.lang.CharSequence p0, int p1, int p2, int p3);

	public void onTextChanged (java.lang.CharSequence p0, int p1, int p2, int p3)
	{
		n_onTextChanged (p0, p1, p2, p3);
	}

	private native void n_onTextChanged (java.lang.CharSequence p0, int p1, int p2, int p3);

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

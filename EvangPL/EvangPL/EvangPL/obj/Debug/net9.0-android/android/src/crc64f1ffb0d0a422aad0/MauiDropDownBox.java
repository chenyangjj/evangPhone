package crc64f1ffb0d0a422aad0;


public class MauiDropDownBox
	extends android.widget.Spinner
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onRestoreInstanceState:(Landroid/os/Parcelable;)V:GetOnRestoreInstanceState_Landroid_os_Parcelable_Handler\n" +
			"";
		mono.android.Runtime.register ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", MauiDropDownBox.class, __md_methods);
	}

	public MauiDropDownBox (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3, int p4, android.content.res.Resources.Theme p5)
	{
		super (p0, p1, p2, p3, p4, p5);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib:Android.Widget.SpinnerMode, Mono.Android:Android.Content.Res.Resources+Theme, Mono.Android", this, new java.lang.Object[] { p0, p1, p2, p3, p4, p5 });
		}
	}

	public MauiDropDownBox (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3, int p4)
	{
		super (p0, p1, p2, p3, p4);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib:Android.Widget.SpinnerMode, Mono.Android", this, new java.lang.Object[] { p0, p1, p2, p3, p4 });
		}
	}

	public MauiDropDownBox (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:Android.Widget.SpinnerMode, Mono.Android", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public MauiDropDownBox (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public MauiDropDownBox (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public MauiDropDownBox (android.content.Context p0, int p1)
	{
		super (p0, p1);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Widget.SpinnerMode, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public MauiDropDownBox (android.content.Context p0)
	{
		super (p0);
		if (getClass () == MauiDropDownBox.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiDropDownBox, EvangMobibrary", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void onRestoreInstanceState (android.os.Parcelable p0)
	{
		n_onRestoreInstanceState (p0);
	}

	private native void n_onRestoreInstanceState (android.os.Parcelable p0);

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

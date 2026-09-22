package crc64f1ffb0d0a422aad0;


public class MauiTimePicker
	extends android.widget.TextView
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("EvangSol.Mobibrary.Platforms.Android.MauiTimePicker, EvangMobibrary", MauiTimePicker.class, __md_methods);
	}

	public MauiTimePicker (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == MauiTimePicker.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiTimePicker, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public MauiTimePicker (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == MauiTimePicker.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiTimePicker, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public MauiTimePicker (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == MauiTimePicker.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiTimePicker, EvangMobibrary", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public MauiTimePicker (android.content.Context p0)
	{
		super (p0);
		if (getClass () == MauiTimePicker.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.MauiTimePicker, EvangMobibrary", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

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

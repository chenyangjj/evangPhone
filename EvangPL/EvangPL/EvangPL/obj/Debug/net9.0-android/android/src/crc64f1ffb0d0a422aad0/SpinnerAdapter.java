package crc64f1ffb0d0a422aad0;


public class SpinnerAdapter
	extends android.widget.ArrayAdapter
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", SpinnerAdapter.class, __md_methods);
	}

	public SpinnerAdapter (android.content.Context p0, int p1)
	{
		super (p0, p1);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public SpinnerAdapter (android.content.Context p0, int p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public SpinnerAdapter (android.content.Context p0, int p1, java.lang.Object[] p2)
	{
		super (p0, p1, p2);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib:T[], Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public SpinnerAdapter (android.content.Context p0, int p1, int p2, java.lang.Object[] p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib:T[], Mono.Android", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public SpinnerAdapter (android.content.Context p0, int p1, java.util.List p2)
	{
		super (p0, p1, p2);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib:System.Collections.Generic.IList`1<T>, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public SpinnerAdapter (android.content.Context p0, int p1, int p2, java.util.List p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == SpinnerAdapter.class) {
			mono.android.TypeManager.Activate ("EvangSol.Mobibrary.Platforms.Android.SpinnerAdapter, EvangMobibrary", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib:System.Collections.Generic.IList`1<T>, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
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

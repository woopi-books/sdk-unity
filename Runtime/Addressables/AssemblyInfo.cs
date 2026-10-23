using UnityEngine.Scripting;

// WoopiContentUrl installs itself from a RuntimeInitializeOnLoadMethod and nothing in a book references it,
// so without this the managed code stripper removes the whole assembly from WebGL builds and the content
// URL passed by the host is silently never applied (bundles are requested from the placeholder host).
[assembly: AlwaysLinkAssembly]

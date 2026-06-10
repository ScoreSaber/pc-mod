using SiraUtil.Tools.FPFC;
using SiraUtil.Zenject;
using System;

namespace ScoreSaber.Core.Compat {
    // not technically needed but newer SiraUtil marks these obsolete and the warnings are bothering me
    internal static class SiraUtilCompat {
#pragma warning disable CS0618
        internal static void AddChangedListener(this IFPFCSettings settings, Action<IFPFCSettings> handler) {
            settings.Changed += handler;
        }

        internal static void RemoveChangedListener(this IFPFCSettings settings, Action<IFPFCSettings> handler) {
            settings.Changed -= handler;
        }

        internal static void ExposeFromContract<T>(this Zenjector zenjector, string contractName) {
            zenjector.Expose<T>(contractName);
        }
#pragma warning restore CS0618
    }
}

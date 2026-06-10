using BeatSaberMarkupLanguage;
using ScoreSaber.Core.Compat;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber.Core.Presentation {
    internal class ScoreSaberUIMaterials {
        private const string RoundedImageMaterialName = "UINoGlowRoundEdge";
        private const string FurryMaterialResource = "ScoreSaber.Resources.cyanisa.furry";
        private const string FurryMaterialName = "FurMat";

        private Material _roundedImageMaterial;
        private Material _furryFontMaterial;
        private Material _defaultFontMaterial;

        internal Material RoundedImageMaterial {
            get {
                if (_roundedImageMaterial == null) {
                    _roundedImageMaterial = Resources.FindObjectsOfTypeAll<Material>()
                        .FirstOrDefault(material => material.name == RoundedImageMaterialName);
                }

                return _roundedImageMaterial ?? Utilities.ImageResources.NoGlowMat;
            }
        }

        internal Material DefaultFontMaterial {
            get {
                if (_defaultFontMaterial == null) {
                    _defaultFontMaterial = BeatSaberUI.MainTextFont.material;
                }

                return _defaultFontMaterial;
            }
        }

        internal async Task<Material> GetFurryFontMaterial() {
            if (_furryFontMaterial != null) {
                return _furryFontMaterial;
            }

            AssetBundle bundle = null;
            IEnumerator LoadBundle() {
                var bundleContainer = AssetBundle.LoadFromMemoryAsync(BsmlCompat.GetResource(Assembly.GetExecutingAssembly(), FurryMaterialResource));
                yield return bundleContainer;
                bundle = bundleContainer.assetBundle;
            }

            await IPA.Utilities.Async.Coroutines.AsTask(LoadBundle());
            _furryFontMaterial = new Material(bundle.LoadAsset<Material>(FurryMaterialName));
            bundle.Unload(false);
            _furryFontMaterial.mainTexture = DefaultFontMaterial.mainTexture;
            return _furryFontMaterial;
        }
    }
}

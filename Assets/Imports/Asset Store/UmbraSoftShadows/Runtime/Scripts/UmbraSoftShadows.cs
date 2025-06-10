using UnityEngine;

namespace Umbra {

    public enum ContactShadowsSource {
        DirectionalLight,
        PointLights
    }

    [ExecuteAlways]
    [HelpURL("https://kronnect.com/guides-category/umbra-soft-shadows")]
    public class UmbraSoftShadows : MonoBehaviour {

        [Tooltip("Currently used umbra profile with settings")]
        public UmbraProfile profile;

        [Tooltip("Source of contact shadows")]
        public ContactShadowsSource contactShadowsSource = ContactShadowsSource.DirectionalLight;

        [Tooltip("Object whose position is used to determine if it's inside point light volumes")]
        public Transform pointLightsTrigger;

        public bool debugShadows;

        public static bool installed;
        public static bool isDeferred;
        public static UmbraSoftShadows instance;


        private void OnEnable () {
            CheckProfile();
            instance = this;
        }

        private void Start () {
            // Set default point lights trigger to main camera if null
            if (pointLightsTrigger == null && Camera.main != null) {
                pointLightsTrigger = Camera.main.transform;
            }
        }

        private void OnDisable () {
            UmbraRenderFeature.UnregisterUmbraLight(this);
            instance = null;
        }

        void OnValidate () {
            CheckProfile();
        }

        private void Reset () {
            CheckProfile();
        }

        void CheckProfile () {
            if (profile == null) {
                profile = ScriptableObject.CreateInstance<UmbraProfile>();
                profile.name = "New Umbra Profile";
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            UmbraRenderFeature.RegisterUmbraLight(this);
        }

    }

}
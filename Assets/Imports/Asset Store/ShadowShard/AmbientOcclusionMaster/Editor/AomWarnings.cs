using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEditor;
using UnityEditor.Rendering;

namespace ShadowShard.AmbientOcclusionMaster.Editor
{
    internal static class AomWarnings
    {
        internal static void DisplayAoSettingsWarningIfNeeded(
            SerializedDataParameter intensity,
            SerializedDataParameter radius,
            SerializedDataParameter falloff,
            AmbientOcclusionMode aoMode,
            AmbientOcclusionMode ao)
        {
            string aoWarningMessage = GetSettingsWarningMessage(ao);
            DisplayAoWarningIfNeeded(intensity, radius, falloff, aoMode, ao, aoWarningMessage);
        }

        private static void DisplayAoWarningIfNeeded(
            SerializedDataParameter intensity,
            SerializedDataParameter radius,
            SerializedDataParameter falloff,
            AmbientOcclusionMode aoMode,
            AmbientOcclusionMode ao,
            string message)
        {
            bool isAoValid = aoMode == ao && AreSettingsValid(intensity, radius, falloff);

            if (!isAoValid)
                DisplayWarning(message);
        }

        internal static void DisplayHdaoInfo(AmbientOcclusionMode aoMode, string message)
        {
            bool isHdao = aoMode == AmbientOcclusionMode.HDAO;

            if (isHdao)
                DisplayInfo(message);
        }

        private static string GetSettingsWarningMessage(AmbientOcclusionMode ao) =>
            $"{ao.ToString()} settings have zero or less than zero values for Intensity, Radius or Falloff. Ambient occlusion does not work with these values";

        private static bool AreSettingsValid(
            SerializedDataParameter intensity,
            SerializedDataParameter radius,
            SerializedDataParameter falloff)
        {
            return intensity.value.floatValue > 0.0f &&
                   radius.value.floatValue > 0.0f &&
                   falloff.value.floatValue > 0.0f;
        }

        private static void DisplayInfo(string message) =>
            EditorGUILayout.HelpBox(message, MessageType.Info);

        private static void DisplayWarning(string message) =>
            EditorGUILayout.HelpBox(message, MessageType.Warning);
    }
}
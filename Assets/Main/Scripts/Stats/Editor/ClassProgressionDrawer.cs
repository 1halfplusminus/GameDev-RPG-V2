

namespace RPG.Stats
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.UIElements;
    using static RPG.Stats.ProgressionAsset;

    [CustomPropertyDrawer(typeof(ClassProgression))]
    public class ClassProgressionDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var handle = Addressables.LoadAssetAsync<VisualTreeAsset>("ClassPregressionDrawer.uxml");
            handle.WaitForCompletion();
            var visualTreeAsset = handle.Result;
            var root = visualTreeAsset.Instantiate();

            var maxLevelProperty = property.FindPropertyRelative(nameof(ClassProgression.MaxLevel));
            var maxLevelField = root.Q<SliderInt>(nameof(ClassProgression.MaxLevel));
            var progressionCurveProperties = GetProgressionCurveProperties(property);
            maxLevelField.RegisterValueChangedCallback((e) =>
            {
                foreach (var progressionProperty in progressionCurveProperties)
                {
                    UpdateAnimationCurveMaxValue(progressionProperty, maxLevelProperty.intValue);
                    property.serializedObject.ApplyModifiedProperties();
                }
            });
            foreach (var progressionProperty in progressionCurveProperties)
            {
                DefaultAnimationCurve(progressionProperty, maxLevelProperty.intValue);
                InitProgressionCurveField(root, property, progressionProperty);
            }

            return root;
        }
        private List<SerializedProperty> GetProgressionCurveProperties(SerializedProperty property)
        {
            var list = new List<SerializedProperty>();
            list.Add(property.FindPropertyRelative(nameof(ClassProgression.Health)));
            list.Add(property.FindPropertyRelative(nameof(ClassProgression.RewardExperience)));
            return list;
        }
        private void InitProgressionCurveField(VisualElement root, SerializedProperty property, SerializedProperty progressionProperty)
        {

            var maxLevelProperty = property.FindPropertyRelative(nameof(ClassProgression.MaxLevel));
            Debug.Log(progressionProperty.name);
            var progressionField = root.Q<VisualElement>(progressionProperty.name);
            var progressionMaxValueField = progressionField.Q<FloatField>("MaxValue");
            var progressionMinValueField = progressionField.Q<FloatField>("MinValue");
            var curveField = progressionField.Q<CurveField>();
            progressionMinValueField.RegisterValueChangedCallback((e) =>
            {
                UpdateAnimationCurveMinValue(progressionProperty);
            });
            progressionMaxValueField.RegisterValueChangedCallback((e) =>
           {
               Debug.Log($"Change max value");
               UpdateAnimationCurveMaxValue(progressionProperty, maxLevelProperty.intValue);

           });
        }
        private void DefaultAnimationCurve(SerializedProperty property, int maxLevel, float maxValue, float minValue)
        {
            var curveProperty = property.FindPropertyRelative("Curve");
            if (curveProperty.animationCurveValue.keys.Length == 0)
            {
                Debug.Log($"Default Is Not Override {maxLevel} {maxValue} ");
                curveProperty.animationCurveValue = AnimationCurve.Linear(0, minValue, maxLevel, maxValue);
                curveProperty.serializedObject.ApplyModifiedProperties();

            }
        }
        private void DefaultAnimationCurve(SerializedProperty property, int maxLevel)
        {

            var maxValueProperty = GetMaxValueProperty(property);
            var minValueProperty = GetMinValueProperty(property);
            DefaultAnimationCurve(property, maxLevel, maxValueProperty.floatValue, minValueProperty.floatValue);
        }

        private static SerializedProperty GetMaxValueProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("MaxValue");
        }
        private static SerializedProperty GetMinValueProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("MinValue");
        }



        private static SerializedProperty GetCurveProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("Curve");
        }
        private void UpdateAnimationCurveMinValue(SerializedProperty property)
        {
            var curveProperty = GetCurveProperty(property);
            var minValueProperty = GetMinValueProperty(property);
            var curve = UpdateAnimationCurve(curveProperty.animationCurveValue, 0, 0, minValueProperty.floatValue);
            SerializeCurveProperty(curveProperty, curve);
        }

        private static void SerializeCurveProperty(SerializedProperty curveProperty, AnimationCurve curve)
        {
            curveProperty.animationCurveValue = curve;
            curveProperty.serializedObject.ApplyModifiedProperties();
        }

        private void UpdateAnimationCurveMaxValue(SerializedProperty property, int maxLevel)
        {
            var curveProperty = GetCurveProperty(property);
            var maxValueProperty = GetMaxValueProperty(property);
            var curve = UpdateAnimationCurve(curveProperty.animationCurveValue, curveProperty.animationCurveValue.length - 1, maxLevel, maxValueProperty.floatValue);
            SerializeCurveProperty(curveProperty, curve);
        }
        private AnimationCurve UpdateAnimationCurve(AnimationCurve curve, int index, int time, float value)
        {
            if (curve.length > 0)
            {
                var keyFrame = curve.keys[index];
                keyFrame.value = value;
                keyFrame.time = time;
                Debug.Log($"Moving Key For Animation Curve time:{time} value: {value}");
                var newIndex = curve.MoveKey(index, keyFrame);
                keyFrame = curve.keys[newIndex];
                Debug.Log($"After Moving Key For Animation Curve time:{keyFrame.time} value: {keyFrame.value}");

            }
            return curve;
        }
    }
}
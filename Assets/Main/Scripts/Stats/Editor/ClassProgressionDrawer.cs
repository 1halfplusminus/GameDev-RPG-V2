#if UNITY_EDITOR

namespace RPG.Stats
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.UIElements;
    using static RPG.Stats.ProgressionAsset;
    using RPG.Core;
    [CustomPropertyDrawer(typeof(ProgressionCurve))]
    public class ProgressionCurveDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var mainTemplateHandle = Addressables.LoadAssetAsync<VisualTreeAsset>("ProgressionCurveDrawer");
            mainTemplateHandle.WaitForCompletion();
            var mainTemplateTreeAsset = mainTemplateHandle.Result;
            var root = mainTemplateTreeAsset.Instantiate();

            var maxLevelProperty = property.FindParentProperty().FindParentProperty().FindPropertyRelative(nameof(ClassProgression.MaxLevel));
            DefaultAnimationCurve(property, maxLevelProperty.intValue);
            InitProgressionCurveField(root, maxLevelProperty, property);


            return root;
        }

        private static void InitProgressionCurveField(VisualElement root, SerializedProperty maxLevelProperty, SerializedProperty progressionProperty)
        {

            Debug.Log(progressionProperty.name);
            var progressionMaxValueField = root.Q<FloatField>("MaxValue");
            var progressionMinValueField = root.Q<FloatField>("MinValue");
            var curveField = root.Q<CurveField>();
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
        private static void DefaultAnimationCurve(SerializedProperty property, int maxLevel, float maxValue, float minValue)
        {
            var curveProperty = property.FindPropertyRelative("Curve");
            if (curveProperty.animationCurveValue.keys.Length == 0)
            {
                Debug.Log($"Default Is Not Override {maxLevel} {maxValue} ");
                curveProperty.animationCurveValue = AnimationCurve.Linear(0, minValue, maxLevel, maxValue);
                curveProperty.serializedObject.ApplyModifiedProperties();

            }
        }
        private static void DefaultAnimationCurve(SerializedProperty property, int maxLevel)
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
        public static void UpdateAnimationCurveMinValue(SerializedProperty property)
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

        public static void UpdateAnimationCurveMaxValue(SerializedProperty property, int maxLevel)
        {
            var curveProperty = GetCurveProperty(property);
            var maxValueProperty = GetMaxValueProperty(property);
            var curve = UpdateAnimationCurve(curveProperty.animationCurveValue, curveProperty.animationCurveValue.length - 1, maxLevel, maxValueProperty.floatValue);
            SerializeCurveProperty(curveProperty, curve);
        }
        private static AnimationCurve UpdateAnimationCurve(AnimationCurve curve, int index, int time, float value)
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
    [CustomPropertyDrawer(typeof(ClassProgression))]
    public class ClassProgressionDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var mainTemplateHandle = Addressables.LoadAssetAsync<VisualTreeAsset>("ClassProgressionDrawer");
            mainTemplateHandle.WaitForCompletion();
            var mainTemplateTreeAsset = mainTemplateHandle.Result;
            var root = mainTemplateTreeAsset.Instantiate();
            var maxLevelProperty = property.FindPropertyRelative(nameof(ClassProgression.MaxLevel));
            var maxLevelField = root.Q<SliderInt>(nameof(ClassProgression.MaxLevel));
            var progressionCurveProperties = GetProgressionCurveProperties(property);
            maxLevelField.RegisterValueChangedCallback((e) =>
            {
                foreach (var progressionProperty in progressionCurveProperties)
                {
                    ProgressionCurveDrawer.UpdateAnimationCurveMaxValue(progressionProperty, maxLevelProperty.intValue);
                    property.serializedObject.ApplyModifiedProperties();
                }
            });
            // foreach (var progressionProperty in progressionCurveProperties)
            // {
            //     DefaultAnimationCurve(progressionProperty, maxLevelProperty.intValue);
            //     InitProgressionCurveField(root, property, progressionProperty);
            // }

            return root;
        }
        // private VisualTreeAsset LoadProgressionCurveTemplate()
        // {
        //     var handle = Addressables.LoadAssetAsync<VisualTreeAsset>("");
        //     handle.WaitForCompletion();
        //     return handle.Result;
        // }
        private List<SerializedProperty> GetProgressionCurveProperties(SerializedProperty property)
        {
            var list = new List<SerializedProperty>();
            var statsProperties = property.FindPropertyRelative(nameof(ClassProgression.Stats));
            for (int i = 0; i < statsProperties.arraySize; i++)
            {
                list.Add(statsProperties.GetArrayElementAtIndex(i));
            }
            return list;

        }
    }
}

#endif
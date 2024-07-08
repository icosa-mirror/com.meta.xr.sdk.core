/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using static Meta.XR.BuildingBlocks.Editor.VariantAttribute;
using static Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.BuildingBlocks.Editor
{
    internal abstract class VariantHandle
    {
        private const BindingFlags BindingFLags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public MemberInfo MemberInfo { get; }
        public VariantAttribute Attribute { get; }
        public InstallationRoutine Owner { get; }

        protected abstract Type Type { get; }
        public abstract object RawValue { get; set; }

        private Func<bool> _condition;
        public static readonly Func<bool> DefaultCondition = () => true;
        public Func<bool> Condition => _condition ??= FetchConditionDelegate();

        protected VariantHandle(MemberInfo memberInfo, VariantAttribute attribute, InstallationRoutine owner)
        {
            MemberInfo = memberInfo;
            Attribute = attribute;
            Owner = owner;
        }

        private Func<bool> FetchConditionDelegate()
        {
            var conditionMethodName = Attribute.Condition;
            var conditionMethod = string.IsNullOrEmpty(conditionMethodName) ? null :
                Owner.GetType().GetMethod(conditionMethodName, VariantHandle.BindingFLags);
            return conditionMethod?.CreateDelegate(typeof(Func<bool>), Owner) as Func<bool> ?? DefaultCondition;
        }

        public static VariantHandle CreateFromRoutine(MemberInfo member, VariantAttribute attribute, InstallationRoutine owner)
        {
            var valueType = GetType(member);
            if (valueType == null) return null;

            var variantType = typeof(VariantForRoutine<>).MakeGenericType(valueType);
            var creationMethod = variantType.GetMethod("FromOwner", BindingFLags);
            return creationMethod?.Invoke(null, new object[] { member, attribute, owner }) as VariantHandle;
        }

        private static Type GetType(MemberInfo memberInfo)
        {
            switch (memberInfo?.MemberType)
            {
                case MemberTypes.Field:
                {
                    var field = memberInfo as FieldInfo;
                    return field?.FieldType;
                }

                case MemberTypes.Property:
                {
                    var property = memberInfo as PropertyInfo;
                    return property?.PropertyType;
                }
            }

            return null;
        }

        public bool Matches(VariantHandle variantHandle)
            => variantHandle.MemberInfo.Name == MemberInfo.Name
           && variantHandle.Attribute.Group == Attribute.Group
           && variantHandle.Attribute.Behavior == Attribute.Behavior;

        public bool Fits(VariantHandle variant)
            => Matches(variant)
               && (variant.Attribute.Behavior == VariantBehavior.Parameter || Equals(variant.RawValue, RawValue));

        public abstract string ToJson();
        public abstract VariantHandle ToSelection(bool forceValue = true);
        public abstract void DrawGUI(SerializedObject serializedObject = null);

        internal static IReadOnlyList<VariantHandle> FetchVariants(InstallationRoutine routine, VariantBehavior behavior)
        {
            var variants = new List<VariantHandle>();
            foreach (var member in routine.GetType().GetMembers(VariantHandle.BindingFLags))
            {
                var attribute = member.GetCustomAttribute<VariantAttribute>();
                if (attribute == null || attribute.Behavior != behavior) continue;

                var variantHandle = VariantHandle.CreateFromRoutine(member, attribute, routine);
                if (variantHandle == null) continue;

                variants.Add(variantHandle);
            }

            return variants;
        }
    }

    internal abstract class VariantHandle<T> : VariantHandle
    {
        public override object RawValue
        {
            get => Value;
            set => Value = (T)value;
        }

        protected abstract T Value { get; set; }
        protected override Type Type => typeof(T);
        protected abstract bool SetValueOnGUI { get; }

        protected VariantHandle(MemberInfo memberInfo, VariantAttribute attribute, InstallationRoutine owner)
            : base(memberInfo, attribute, owner)
        {
        }

        public override string ToJson() => JsonUtility.ToJson(Value);
        public override VariantHandle ToSelection(bool forceValue = true) => new VariantSelection<T>(this, forceValue);

        public override void DrawGUI(SerializedObject serializedObject = null)
        {
            using var disabledScope = new EditorGUI.DisabledScope(!Condition());
            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);
            EditorGUILayout.BeginHorizontal();

            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(MemberInfo.Name));
            }
            else
            {
                var name = ObjectNames.NicifyVariableName(MemberInfo.Name);
                EditorGUILayout.LabelField(name, Styles.GUIStyles.LabelStyle, GUILayout.Width(Constants.LabelWidth));
                switch (Value)
                {
                    case int intValue:
                        ApplyValue((T)(object)EditorGUILayout.IntField(intValue));
                        break;
                    case string stringValue:
                        ApplyValue((T)(object)EditorGUILayout.TextField(stringValue));
                        break;
                    case bool boolValue:
                        ApplyValue((T)(object)EditorGUILayout.Toggle(boolValue));
                        break;
                    case Enum enumValue:
                        ApplyValue((T)(object)EditorGUILayout.EnumPopup(enumValue));
                        break;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(Attribute.Description))
            {
                EditorGUILayout.LabelField(Attribute.Description, Styles.GUIStyles.InfoStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyValue(T value)
        {
            if (!SetValueOnGUI) return;

            Value = value;
        }
    }

    /// <summary>
    /// VariantHandle tightly attached to an InstallationRoutine
    /// Used to store a Value for the Variant
    /// </summary>
    internal class VariantForRoutine<T> : VariantHandle<T>
    {
        protected override bool SetValueOnGUI => false;

        protected override T Value
        {
            get
            {
                switch (MemberInfo?.MemberType)
                {
                    case MemberTypes.Field:
                    {
                        var field = MemberInfo as FieldInfo;
                        return (T)field?.GetValue(Owner);
                    }

                    case MemberTypes.Property:
                    {
                        var property = MemberInfo as PropertyInfo;
                        return (T)property?.GetValue(Owner);
                    }
                }

                return default(T);
            }
            set
            {
                switch (MemberInfo?.MemberType)
                {
                    case MemberTypes.Field:
                    {
                        var field = MemberInfo as FieldInfo;
                        field?.SetValue(Owner, value);
                        break;
                    }

                    case MemberTypes.Property:
                    {
                        var property = MemberInfo as PropertyInfo;
                        property?.SetValue(Owner, value);
                        break;
                    }
                }
            }
        }

        private VariantForRoutine(MemberInfo memberInfo, VariantAttribute attribute, InstallationRoutine owner)
            : base(memberInfo, attribute, owner)
        {
        }

        internal static VariantForRoutine<T> FromOwner(MemberInfo member, VariantAttribute attribute, InstallationRoutine owner)
        {
            return new VariantForRoutine<T>(member, attribute, owner);
        }
    }

    /// <summary>
    /// VariantHandle not specifically attached to an InstallationRoutine
    /// Used to store a Value for the Variant
    /// </summary>
    internal class VariantSelection<T> : VariantHandle<T>
    {
        protected override bool SetValueOnGUI => true;

        protected sealed override T Value { get; set; }

        public VariantSelection(VariantHandle source, bool forceValue)
            : base(source.MemberInfo, source.Attribute, source.Owner)
        {
            var copyValue = forceValue || (source.Attribute.Default == null);
            Value = copyValue ? (T)source.RawValue : (T)source.Attribute.Default;
        }
    }
}

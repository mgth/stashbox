﻿using Stashbox.Attributes;
using Stashbox.Configuration;
using Stashbox.Registration;
using Stashbox.Resolution;
using Stashbox.Resolution.Resolvers;
using Stashbox.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DependencyAttribute = Stashbox.Attributes.DependencyAttribute;

#if NET40
namespace System.Reflection
{
    internal static class TypeExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type)
        {
            return new TypeInfo(type);
        }

        public static TAttribute GetCustomAttribute<TAttribute>(this TypeInfo typeInfo) where TAttribute : Attribute
        {
            var attrType = typeof(TAttribute);
            var attributes = typeInfo.GetCustomAttributes(attrType, false);
            return (TAttribute)attributes.FirstOrDefault();
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo parameter) =>
            parameter.GetCustomAttributes(false).Cast<Attribute>();

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo property) =>
            property.GetCustomAttributes(false).Cast<Attribute>();
    }
}
#endif

namespace System
{
    internal static class TypeExtensions
    {
        public static Type GetEnumerableType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsArray)
                return type.GetElementType();

            if (typeInfo.ImplementsGenericType(typeof(IEnumerable<>)) && type != typeof(string) && typeInfo.GenericTypeArguments.Length == 1)
                return typeInfo.GenericTypeArguments[0];

            return null;
        }

        public static bool IsGenericType(this Type type) =>
           type.GetTypeInfo().IsGenericType;

        public static bool IsClosedGenericType(this Type type) =>
            type.GetTypeInfo().IsClosedGenericType();

        public static bool IsClosedGenericType(this TypeInfo typeInfo) =>
            typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition;

        public static bool IsOpenGenericType(this Type type) =>
            type.GetTypeInfo().IsOpenGenericType();

        public static bool IsOpenGenericType(this TypeInfo typeInfo) =>
            typeInfo.IsGenericType && typeInfo.ContainsGenericParameters;

        public static DependencyAttribute GetDependencyAttribute(this MemberInfo property)
        {
            var attr = property.GetCustomAttributes(Constants.DependencyAttributeType, false)?.FirstOrDefault();
            return (DependencyAttribute)attr;
        }

        public static DependencyAttribute GetDependencyAttribute(this ParameterInfo parameter)
        {
            var attr = parameter.GetCustomAttributes(Constants.DependencyAttributeType, false)?.FirstOrDefault();
            return (DependencyAttribute)attr;
        }

        public static InjectionMethodAttribute GetInjectionAttribute(this MemberInfo method)
        {
            var attr = method.GetCustomAttributes(Constants.InjectionAttributeType, false)?.FirstOrDefault();
            return (InjectionMethodAttribute)attr;
        }

        public static Type[] GetGenericArguments(this Type type) =>
            type.GetTypeInfo().GenericTypeArguments;

        public static ConstructorInfo GetConstructor(this Type type, params Type[] args) =>
            type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Select(p => p.ParameterType).SequenceEqual(args));

        public static bool IsBackingField(this MemberInfo field) =>
            field.Name[0] == '<';

        public static bool IsIndexer(this PropertyInfo property) =>
            property.GetIndexParameters().Length != 0;

        public static bool HasDefaultValue(this ParameterInfo parameter) =>
            parameter.IsOptional;

        public static bool HasPublicParameterlessConstructor(this TypeInfo info) =>
            info.DeclaredConstructors.FirstOrDefault(c => c.IsPublic && c.GetParameters().Length == 0) != null;

        public static MethodInfo GetSingleMethod(this Type type, string name)
        {
            var found = type.GetSingleMethodOrDefault(name);
            if (found == null)
                throw new InvalidOperationException($"'{name}' method not found on {type.FullName}.");

            return found;
        }

        public static MethodInfo GetSingleMethodOrDefault(this Type type, string name) =>
            type.GetTypeInfo().GetDeclaredMethod(name);

        public static bool HasPublicSetMethod(this MemberInfo property)
        {
            var setter = property.GetSetterMethodOrDefault();
            return setter != null && setter.IsPublic;
        }

        public static MethodInfo GetSetterMethodOrDefault(this MemberInfo property) =>
            property.DeclaringType.GetSingleMethodOrDefault("set_" + property.Name);

        public static MethodInfo GetGetterMethodOrDefault(this MemberInfo property) =>
            property.DeclaringType.GetSingleMethodOrDefault("get_" + property.Name);

        public static bool IsDisposable(this Type type) =>
            type.Implements(Constants.DisposableType)
#if HAS_ASYNC_DISPOSABLE
         || type.Implements(Constants.AsyncDisposableType)
#endif
        ;
        
        public static bool IsCompositionRoot(this Type type) =>
            type.Implements(Constants.CompositionRootType);

        public static bool IsResolvableType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return !typeInfo.IsAbstract &&
                !typeInfo.IsInterface &&
                typeInfo.IsClass &&
                type != typeof(string) &&
                typeInfo.GetCustomAttribute<CompilerGeneratedAttribute>() == null;
        }

        public static IEnumerable<Type> GetRegisterableInterfaceTypes(this Type type) =>
            type.GetTypeInfo().ImplementedInterfaces.Where(t => t != Constants.DisposableType);

        public static IEnumerable<Type> GetRegisterableBaseTypes(this Type type)
        {
            var baseType = type.GetTypeInfo().BaseType;
            while (baseType != null && !baseType.IsObjectType())
            {
                yield return baseType;
                baseType = baseType.GetTypeInfo().BaseType;
            }
        }

        public static bool IsObjectType(this Type type) => type == Constants.ObjectType;

        public static bool Implements(this Type type, Type interfaceType) =>
            type == interfaceType || type.GetTypeInfo().Implements(interfaceType);

        private static bool Implements(this TypeInfo typeInfo, Type interfaceType) =>
            interfaceType.IsOpenGenericType()
                ? typeInfo.ImplementsGenericType(interfaceType.GetGenericTypeDefinition())
                : interfaceType.GetTypeInfo().IsAssignableFrom(typeInfo);

        public static bool ImplementsWithoutGenericCheck(this Type type, Type interfaceType) =>
            type.GetTypeInfo().ImplementsWithoutGenericCheck(interfaceType);

        private static bool ImplementsWithoutGenericCheck(this TypeInfo typeInfo, Type interfaceType) =>
            interfaceType.GetTypeInfo().IsAssignableFrom(typeInfo);

        private static bool ImplementsGenericType(this TypeInfo type, Type genericType) =>
            MapsToGenericTypeDefinition(type, genericType) ||
            HasInterfaceThatMapsToGenericTypeDefinition(type, genericType) ||
            type.BaseType != null && type.BaseType.GetTypeInfo().ImplementsGenericType(genericType);

        private static bool HasInterfaceThatMapsToGenericTypeDefinition(TypeInfo type, Type genericType) =>
            type.ImplementedInterfaces
              .Where(it => it.IsGenericType())
              .Any(it => it.GetGenericTypeDefinition() == genericType);

        private static bool MapsToGenericTypeDefinition(TypeInfo type, Type genericType) =>
            genericType.GetTypeInfo().IsGenericTypeDefinition
              && type.IsGenericType
              && type.GetGenericTypeDefinition() == genericType;

        public static bool IsFuncType(this Type type) =>
            type.IsClosedGenericType() && FuncResolver.SupportedTypes.Contains(type.GetGenericTypeDefinition());

        public static ConstructorInfo GetFirstConstructor(this Type type) =>
            type.GetTypeInfo().DeclaredConstructors.FirstOrDefault();

        public static ConstructorInfo GetConstructorByArguments(this Type type, params Type[] types) =>
            (ConstructorInfo)type.GetTypeInfo().DeclaredConstructors.GetMethodByArguments(types);

        public static MethodInfo GetMethodByArguments(this Type type, string name, params Type[] types) =>
            (MethodInfo)type.GetTypeInfo().DeclaredMethods.Where(m => m.Name == name).GetMethodByArguments(types);

        public static MethodBase GetMethodByArguments(this IEnumerable<MethodBase> methods, params Type[] types) =>
            methods.FirstOrDefault(constructor =>
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != types.Length)
                    return false;

                var length = parameters.Length;
                for (var i = 0; i < length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var argType = types[i];

                    var eq = paramType == argType;
                    var im = argType.Implements(paramType);

                    if (!eq && !im)
                        return false;
                }

                return true;
            });

        public static TypeInformation AsTypeInformation(this ParameterInfo parameter,
            Type declaringType,
            RegistrationContext registrationContext,
            ContainerConfiguration containerConfiguration)
        {
            var customAttributes = parameter.GetCustomAttributes();
            var dependencyName = parameter.GetDependencyAttribute()?.Name;

            if (registrationContext.DependencyBindings.Count != 0 || containerConfiguration.TreatingParameterAndMemberNameAsDependencyNameEnabled)
            {
                if (registrationContext.DependencyBindings.TryGetValue(parameter.Name,
                    out var foundNamedDependencyName))
                    dependencyName = foundNamedDependencyName;
                else if (registrationContext.DependencyBindings.TryGetValue(parameter.ParameterType,
                    out var foundTypedDependencyName))
                    dependencyName = foundTypedDependencyName;
                else if (dependencyName == null &&
                         containerConfiguration.TreatingParameterAndMemberNameAsDependencyNameEnabled)
                    dependencyName = parameter.Name;
            }

            return new TypeInformation(
                parameter.ParameterType,
                declaringType,
                dependencyName,
                customAttributes,
                parameter.Name,
                parameter.HasDefaultValue(),
                parameter.DefaultValue);
        }

        public static TypeInformation AsTypeInformation(this MemberInfo member,
            RegistrationContext registrationContext,
            ContainerConfiguration containerConfiguration)
        {
            var customAttributes = member.GetCustomAttributes();
            var dependencyName = member.GetDependencyAttribute()?.Name;

            if (registrationContext.DependencyBindings.Count != 0 || containerConfiguration.TreatingParameterAndMemberNameAsDependencyNameEnabled)
            {
                if (registrationContext.DependencyBindings.TryGetValue(member.Name, out var foundNamedDependencyName))
                    dependencyName = foundNamedDependencyName;
                else if (dependencyName == null && containerConfiguration.TreatingParameterAndMemberNameAsDependencyNameEnabled)
                    dependencyName = member.Name;
            }

            var type = member is PropertyInfo prop ? prop.PropertyType : ((FieldInfo)member).FieldType;
            return new TypeInformation(type, member.DeclaringType, dependencyName, customAttributes, member.Name, false, null);
        }

        public static IEnumerable<ConstructorInfo> GetUsableConstructors(this TypeInfo typeInfo) =>
            typeInfo.DeclaredConstructors.Where(constructor => !constructor.IsStatic && constructor.IsPublic);

        public static MethodInfo[] GetUsableMethods(this TypeInfo typeInfo)
        {
            var methods =  typeInfo.DeclaredMethods.Where(method => method.GetInjectionAttribute() != null);
            var baseType = typeInfo.BaseType;
            while (baseType != null && !baseType.IsObjectType())
            {
                var baseTypeInfo = baseType.GetTypeInfo();
                methods =  methods.Concat(baseTypeInfo.DeclaredMethods.Where(method => method.GetInjectionAttribute() != null));
                baseType = baseTypeInfo.BaseType;
            }

            return methods.CastToArray();
        }

        public static MemberInfo[] GetUsableMembers(this TypeInfo typeInfo,
            RegistrationContext contextData,
            ContainerConfiguration containerConfiguration)
        {
            var autoMemberInjectionEnabled = containerConfiguration.AutoMemberInjectionEnabled || contextData.AutoMemberInjectionEnabled;
            var autoMemberInjectionRule = contextData.AutoMemberInjectionEnabled ? contextData.AutoMemberInjectionRule :
                containerConfiguration.AutoMemberInjectionRule;

            var publicPropsEnabled = autoMemberInjectionEnabled && (autoMemberInjectionRule & Rules.AutoMemberInjectionRules.PropertiesWithPublicSetter) == Rules.AutoMemberInjectionRules.PropertiesWithPublicSetter;
            var limitedPropsEnabled = autoMemberInjectionEnabled && (autoMemberInjectionRule & Rules.AutoMemberInjectionRules.PropertiesWithLimitedAccess) == Rules.AutoMemberInjectionRules.PropertiesWithLimitedAccess;
            var fieldsEnabled = autoMemberInjectionEnabled && (autoMemberInjectionRule & Rules.AutoMemberInjectionRules.PrivateFields) == Rules.AutoMemberInjectionRules.PrivateFields;

            IEnumerable<MemberInfo> properties = typeInfo.DeclaredProperties.Where(member => member.FilterProperty(contextData, containerConfiguration, publicPropsEnabled, limitedPropsEnabled));
            IEnumerable<MemberInfo> fields = typeInfo.DeclaredFields.Where(member => member.FilterField(contextData, containerConfiguration, fieldsEnabled));

            var baseType = typeInfo.BaseType;
            while (baseType != null && !baseType.IsObjectType())
            {
                var baseTypeInfo = baseType.GetTypeInfo();
                properties = properties.Concat(baseTypeInfo.DeclaredProperties.Where(member => member.FilterProperty(contextData, containerConfiguration, publicPropsEnabled, limitedPropsEnabled)));
                fields = fields.Concat(baseTypeInfo.DeclaredFields.Where(member => member.FilterField(contextData, containerConfiguration, fieldsEnabled)));
                baseType = baseTypeInfo.BaseType;
            }

            return properties.Concat(fields).CastToArray();
        }

        public static bool SatisfiesGenericConstraintsOf(this Type typeForCheck, TypeInfo against)
        {
            if (!against.IsGenericTypeDefinition) return true;

            var parametersLength = against.GenericTypeParameters.Length;
            var typeForCheckInfo = typeForCheck.GetTypeInfo();
            var argumentsLength = typeForCheckInfo.GenericTypeArguments.Length;

            for (var i = 0; i < parametersLength; i++)
            {
                var paramType = against.GenericTypeParameters[i].GetTypeInfo();
                var parameterPosition = paramType.GenericParameterPosition;
                if (parameterPosition >= argumentsLength)
                    return false;

                var argumentType = typeForCheckInfo.GenericTypeArguments[parameterPosition];
                var argumentForValidation = argumentType.GetTypeInfo();
                var parameterAttributes = paramType.GenericParameterAttributes;

                if (parameterAttributes.HasDefaultConstructorConstraint() &&
                    !argumentForValidation.IsPrimitive &&
                    !argumentForValidation.HasPublicParameterlessConstructor())
                    return false;

                if (parameterAttributes.HasReferenceTypeConstraint() &&
                    !argumentForValidation.IsClass)
                    return false;

                var constraints = paramType.GetGenericParameterConstraints();
                var constraintsLength = constraints.Length;

                if (constraints.Length <= 0) continue;

                var found = false;
                for (var j = 0; j < constraintsLength; j++)
                {
                    var con = constraints[j];
                    var constraintForCheck = con.IsClosedGenericType() ? con.GetGenericTypeDefinition().MakeGenericType(argumentType) : con;
                    if (argumentForValidation.Implements(constraintForCheck))
                        found = true;
                }

                return found;
            }

            return true;
        }

        private static bool FilterProperty(this PropertyInfo prop, RegistrationContext contextData,
            ContainerConfiguration containerConfiguration, bool publicPropsEnabled, bool limitedPropsEnabled)
        {
            var valid = prop.CanWrite && !prop.IsIndexer() &&
                    (prop.GetDependencyAttribute() != null ||
                     publicPropsEnabled && prop.HasPublicSetMethod() || limitedPropsEnabled ||
                     contextData.DependencyBindings.ContainsKey(prop.Name));

            valid = valid && (containerConfiguration.AutoMemberInjectionFilter == null || containerConfiguration.AutoMemberInjectionFilter(prop));
            valid = valid && (contextData.AutoMemberInjectionFilter == null || contextData.AutoMemberInjectionFilter(prop));

            return valid;
        }

        private static bool FilterField(this FieldInfo field, RegistrationContext contextData,
            ContainerConfiguration containerConfiguration, bool fieldsEnabled)
        {
            var valid = !field.IsInitOnly && !field.IsBackingField() &&
                        (field.GetDependencyAttribute() != null ||
                         fieldsEnabled ||
                         contextData.DependencyBindings.ContainsKey(field.Name));

            valid = valid && (containerConfiguration.AutoMemberInjectionFilter == null || containerConfiguration.AutoMemberInjectionFilter(field));
            valid = valid && (contextData.AutoMemberInjectionFilter == null || contextData.AutoMemberInjectionFilter(field));

            return valid;
        }
        public static bool IsNullableType(this Type type) =>
            type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool HasDefaultConstructorConstraint(this GenericParameterAttributes attributes) =>
            (attributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint;

        public static bool HasReferenceTypeConstraint(this GenericParameterAttributes attributes) =>
            (attributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint;

        public static MethodInfo GetMethod(this Delegate @delegate) =>
#if NET40
            @delegate.Method;
#else
            @delegate.GetMethodInfo();
#endif

        public static bool IsCompiledLambda(this Delegate @delegate) =>
            @delegate.Target != null && @delegate.Target.GetType().FullName == "System.Runtime.CompilerServices.Closure";

        public static string GetDiagnosticsView(this Type type)
        {
            var info = type.GetTypeInfo();
            if (info.IsGenericType)
            {
                var typeName = type.Name;
                var i = typeName.IndexOf('`');
                typeName = i != -1 ? typeName.Substring(0, i) : typeName;

                typeName += "<";
                if(info.IsGenericTypeDefinition)
                    typeName += new string(Enumerable.Repeat(',', info.GenericTypeParameters.Length - 1).ToArray());
                else
                    typeName += string.Join(",", info.GenericTypeArguments.Select(a => a.GetDiagnosticsView()));

                typeName += ">";

                return typeName;
            }

            return type.Name;
        }
    }
}
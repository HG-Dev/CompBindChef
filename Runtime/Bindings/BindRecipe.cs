using System;
using System.Runtime.CompilerServices;
using System.Text;
using HG.CompBindChef.Utils;

//[assembly: InternalsVisibleTo(nameof(HG.CompBindChef))]

namespace HG.CompBindChef.Bindings
{
    [Serializable]
    public class BindRecipe : IEquatable<BindRecipe>
    {
        public bool IsMissingTypes => string.IsNullOrWhiteSpace(componentType) || string.IsNullOrWhiteSpace(propertyType);
        public bool IsValid => !IsMissingTypes && !string.IsNullOrWhiteSpace(propSetter);
        public bool HasGetter => !string.IsNullOrWhiteSpace(propGetter);
        public bool HasSetter => !string.IsNullOrWhiteSpace(propSetter);
        public bool HasChangeEvent => !string.IsNullOrWhiteSpace(propChangedEvent);

        public string ComponentTypeShortName => componentType;
        
        public string componentType;
        public string propertyType;
        public string propSetter;
        public string propGetter;
        public string propChangedEvent;

        public int GetPropertyHashCode() => HashCode.Combine(componentType, propertyType, propSetter, propGetter, propChangedEvent);
        
        public bool Equals(BindRecipe other)
        {
            return other != null && GetPropertyHashCode() == other.GetPropertyHashCode();
        }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nameof(BindRecipe));
            sb.Append(": [");
            sb.Append(HasSetter ? "Set " : " -  ");
            sb.Append(HasGetter ? "Get" : " - ");
            sb.Append(HasChangeEvent ? " Evt" : "  - ");
            sb.Append("] ");
            sb.Append(TypeNameConverter.AssemblyQualifiedNameToCodeReadyTypeString(propertyType));
            sb.Append(" on ");
            sb.Append(TypeNameConverter.AssemblyQualifiedNameToCodeReadyTypeString(componentType));
            return sb.ToString();
        }
    }
}
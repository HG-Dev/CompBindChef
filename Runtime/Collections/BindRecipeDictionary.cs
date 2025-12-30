using System.Collections.Generic;
using System.Linq;
using HG.CompBindChef.Bindings;

namespace HG.CompBindChef.Collections
{
    public class BindRecipeDictionary : SerializedKeyedCollection<int, BindRecipe>
    {
        public IEnumerable<KeyValuePair<int, BindRecipe>> ItemsWithComponentType(
            string assemblyQualifiedTypeName)
        {
            foreach (var pair in this)
            {
                if (pair.Value.componentType == assemblyQualifiedTypeName)
                    yield return pair;
            }
        }

        public IEnumerable<IGrouping<string, KeyValuePair<int, BindRecipe>>> ItemsGroupedByComponentType()
        {
            return this.GroupBy(pair => pair.Value.componentType);
        }

        public override int ExtractKey(BindRecipe recipe)
        {
            return recipe.GetHashCode();
        }
    }
}
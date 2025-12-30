using System.Collections.Generic;
using UnityEngine;

namespace HG.CompBindChef.Bindings
{
    public sealed partial class BindRecipeSource : MonoBehaviour
    {
        [SerializeField] internal int[] bindCodes;

        public IReadOnlyList<int> BindCodes => bindCodes;
    }
}
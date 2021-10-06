using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    /// <summary>
    /// Defines randomization logic for a soldier's selectedProps.
    /// </summary>
    public class NPCPropComponent : SerializedMonoBehaviour
    {
        [Tooltip("Maps settings to different prop roots. Props will be instantiated under this root.")]
        [OnValueChanged("UpdateMaterialReference", includeChildren: true)]
        [FoldoutGroup("Prop Randomizer")]
        public Dictionary<string, PropSettings> props = new Dictionary<string, PropSettings>();

        [Tooltip("Materials are randomly selected and applied to each randomized prop.")]
        [FoldoutGroup("Material Randomizer")]
        public List<Material> materials = new List<Material>();

        [LabelText("Meshes are randomly selected and applied to the selected skinned mesh renderer."), FoldoutGroup("Mesh Randomizer")]
        public List<Mesh> meshes = new List<Mesh>();
        
        [FoldoutGroup("Mesh Randomizer")]
        public SkinnedMeshRenderer meshRenderer;

        private void Start()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<SkinnedMeshRenderer>(); // only get if one isn't assigned
            if (!meshRenderer) Debug.Log("No mesh renderer provided, mesh will not be randomized.");
        }

        [Button, FoldoutGroup("Mesh Randomizer")]
        public void RandomizeMesh()
        {
            if (meshRenderer && meshes.Count > 0)
            {
                meshRenderer.sharedMesh = meshes[Random.Range(0, meshes.Count)];
            }
        }        
        
        [Button, FoldoutGroup("Material Randomizer")]
        public void RandomizeMaterial()
        {
            // todo: ugh this is not nice
            transform.GetChild(1).GetComponent<Renderer>().material = materials[Random.Range(0, materials.Count)];
        }

        [Button]
        [FoldoutGroup("Prop Randomizer")]
        public void SetForAll(bool enableRandomize, List<Material> newMaterials)
        {
            foreach (var prop in props)
            {
                prop.Value.randomizable = enableRandomize;
                prop.Value.materials = newMaterials;
            }
        }
        
        [Button]
        public void RandomizeAll()
        {
            // randomize root object's newMaterials
            RandomizeMaterial();
            
            // only runs if mesh is set!
            RandomizeMesh();

            foreach (var prop in props)
            {
                if (prop.Value.randomizable) prop.Value.Randomize();
            }
        }

        /// <summary>
        /// Returns the newly randomized game object instance.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public GameObject Randomize(string prop)
        {
            var propSettings = props[prop];
            propSettings.Randomize();
            return propSettings.activeProp;
        }

        /// <summary>
        /// Returns the newly randomized game object instances
        /// </summary>
        /// <param name="selectedProps"></param>
        /// <returns></returns>
        [Button(ButtonSizes.Large, DisplayParameters = true)]
        [FoldoutGroup("Prop Randomizer")]
        public GameObject[] Randomize(string[] selectedProps)
        {
            var output = new GameObject[selectedProps.Length];

            var i = 0;
            foreach (var prop in selectedProps)
            {
                output[i] = Randomize(prop);
                i++;
            }

            return output;
        }

        private void UpdateMaterialReference()
        {
            foreach (var prop in props)
            {
                if (prop.Value.materials == null)
                {
                    prop.Value.materials = materials;
                }
            }
        }
    }

    [Serializable]
    public class PropSettings
    {
        public bool randomizable = true;
        
        [OnValueChanged("TryGetActiveProp")]
        public Transform parent;
        public GameObject activeProp;
        public bool allowNone = true;
        public List<Material> materials;

        [OnValueChanged("RefreshRandomizerStack", InvokeOnUndoRedo = true, InvokeOnInitialize = true)]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public List<GameObject> otherProps = new List<GameObject>();

        private Stack<GameObject> randomizerStack;

        private Unity.Mathematics.Random _randomizer =
            new Unity.Mathematics.Random((uint) Random.Range(0, Int32.MaxValue));


        public bool TryGetActiveProp()
        {
            if (parent && parent.childCount > 0)
            {
                activeProp = parent.GetChild(0).gameObject;
                return activeProp;
            }

            return false;
        }

        public string[] GetPropNames()
        {
            return otherProps.Select(_ => _.name).ToArray();
        }
        
        internal void RefreshRandomizerStack()
        {
            _randomizer = new Unity.Mathematics.Random((uint) Random.Range(0, Int32.MaxValue));
            var randomized = otherProps.OrderBy(_ => _randomizer.NextInt());
            randomizerStack = new Stack<GameObject>(randomized);
        }

        [Button]
        internal void Randomize()
        {
            if (randomizerStack == null || randomizerStack?.Count == 0) RefreshRandomizerStack();
            if (activeProp)
            {
                if (Application.isEditor) Object.DestroyImmediate(activeProp.gameObject);
                else Object.Destroy(activeProp.gameObject);
                activeProp = null;
            }

            if (allowNone && _randomizer.NextBool())
            {
                // set nothing as object
                activeProp = null;
            }
            else
            {
                activeProp = Object.Instantiate(randomizerStack.Pop(), parent, false);
                if (materials != null)
                    activeProp.GetComponent<Renderer>().material = materials[Random.Range(0, materials.Count)];
                activeProp.transform.localPosition = Vector3.zero;
                activeProp.gameObject.SetActive(true);
            }
        }

        [Button]
        internal void ClearCurrent()
        {
            if (Application.isEditor) Object.DestroyImmediate(activeProp);
            else Object.Destroy(activeProp);

            activeProp = null;
        }

        internal void Select(int index)
        {
            if (activeProp)
            {
                if (Application.isEditor) Object.DestroyImmediate(activeProp.gameObject);
                else Object.Destroy(activeProp.gameObject);
                activeProp = null;
            }

            // if valid index we pick one
            if (index >= 0 && index < otherProps.Count)
            {
                activeProp = Object.Instantiate(otherProps[index], parent, false);
            }
            
            // otherwise no weapon is selected
        }
    }

}
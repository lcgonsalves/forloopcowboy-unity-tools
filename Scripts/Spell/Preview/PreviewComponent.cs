using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    /// <summary>
    /// Component that runs previews.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PreviewComponent : MonoBehaviour
    {
        private bool _previewOn = false;
        private IPreview _currentPreview;
        private LineRenderer _lineRenderer;
        private PreviewContext _context;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _context = new PreviewContext(this, _lineRenderer);
        }

        private void LateUpdate()
        {
            if (_previewOn) _currentPreview?.Update(ref _context);
            else _currentPreview?.Hide(ref _context);
        }

        public void SetPreview(IPreview newPreview)
        {
            _currentPreview?.Dispose(ref _context);
            _currentPreview = newPreview;
        }

        public void SetAndShow(IPreview newPreview)
        {
            SetPreview(newPreview);
            _previewOn = true;
        }

        public void Hide() => _previewOn = false;
        public void Show() => _previewOn = true;

    }
    
    /// <summary>
    /// Contextual information regarding the preview component.
    /// </summary>
    public readonly struct PreviewContext
    {
        public PreviewComponent PreviewComponent { get; }
        public LineRenderer LineRenderer { get; }

        public PreviewContext(PreviewComponent previewComponent, LineRenderer lineRenderer)
        {
            LineRenderer = lineRenderer;
            PreviewComponent = previewComponent;
        }
    }

    /// <summary>
    /// Interface that allows for creating custom previewing functions.
    /// You can activate this preview by calling SetAndShow() or SetPreview() on an instance
    /// of PreviewComponent.
    /// </summary>
    public interface IPreview
    {
        /// <summary>Frame By Frame update function.</summary>
        void Update(ref PreviewContext context);

        /// <summary>Called when preview is switched off.</summary>
        void Dispose(ref PreviewContext context);

        /// <summary>Called when preview is not enabled on update loop.</summary>
        void Hide(ref PreviewContext context);
    }
}
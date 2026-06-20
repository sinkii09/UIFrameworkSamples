using UnityEngine;

namespace AircraftStriker
{
    public class BackgroundScrollController : MonoBehaviour
    {
        [SerializeField] private Transform _tileA;
        [SerializeField] private Transform _tileB;
        [SerializeField] private float     _scrollSpeed = 2f;
        [SerializeField] private float     _tileHeight  = 20f;

        private void Update()
        {
            _tileA.position += _scrollSpeed * Time.deltaTime * Vector3.down;
            _tileB.position += _scrollSpeed * Time.deltaTime * Vector3.down;

            if (_tileA.position.y < -_tileHeight)
                _tileA.position = _tileB.position + Vector3.up * _tileHeight;
            else if (_tileB.position.y < -_tileHeight)
                _tileB.position = _tileA.position + Vector3.up * _tileHeight;
        }
    }
}

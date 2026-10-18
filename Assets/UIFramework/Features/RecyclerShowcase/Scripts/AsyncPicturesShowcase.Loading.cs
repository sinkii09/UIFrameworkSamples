using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RecyclerShowcase
{
    /// <summary>The two ways tab 5 loads a picture: the naive bug and the correct bind.</summary>
    public partial class AsyncPicturesShowcase
    {
        // The bug, on purpose: nothing cleared, so a recycled row keeps the last item's picture until
        // this lands; nothing cancelled or checked, so a slow load for a previous item lands last and
        // wins. The panel-wide token is demo hygiene only — it stops naive loads still in flight from
        // landing after a switch to Correct and being blamed on it.
        private async UniTaskVoid NaiveLoadAsync(AsyncPictureCell cell, int key)
        {
            Color picture;
            try
            {
                picture = await _loader.LoadAsync(key, _naiveLoads.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cell != null) cell.ShowPicture(key, picture); // null once play mode has ended
        }

        private void BindCorrect(AsyncPictureCell cell, int key)
        {
            // Synchronous on a hit, so a known picture never flashes the placeholder for a frame.
            if (_loader.TryGetCached(key, out Color cached))
            {
                cell.ShowPicture(key, cached);
                return;
            }

            // Cleared now, not when the load lands: a cell re-rented this frame is still on screen
            // showing the previous item's picture.
            _placeholders++;
            cell.ShowPlaceholder();
            CorrectLoadAsync(cell, key, cell.BindCancellationToken).Forget();
        }

        private async UniTaskVoid CorrectLoadAsync(AsyncPictureCell cell, int key, CancellationToken bindToken)
        {
            while (true)
            {
                try
                {
                    Color picture = await _loader.LoadAsync(key, bindToken);

                    // Not cancelled means not recycled, so the cell still represents this key.
                    if (!bindToken.IsCancellationRequested) cell.ShowPicture(key, picture);
                    return;
                }
                catch (OperationCanceledException)
                {
                    if (bindToken.IsCancellationRequested) return; // the cell moved on: expected
                    // The shared load it joined was cancelled under it (Clear cache): load again.
                }
            }
        }
    }
}

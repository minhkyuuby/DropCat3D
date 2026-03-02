using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed partial class InventoryGrid3D
    {
        public void SetBoundaryMaskEnabled(bool enabled)
        {
            useBoundaryMask = enabled;
        }

        public bool GetBoundaryMaskCell(int x, int y)
        {
            EnsureBoundaryMask();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            return GetMaskCell(x, y);
        }

        public void SetBoundaryMaskCell(int x, int y, bool enabled)
        {
            EnsureBoundaryMask();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            boundaryMask[y * width + x] = enabled;
        }

        private void EnsureBoundaryMask()
        {
            int size = Mathf.Max(0, width * height);
            if (boundaryMask == null || boundaryMask.Length != size || maskWidth != width || maskHeight != height)
            {
                var oldMask = boundaryMask;
                int oldWidth = maskWidth;
                int oldHeight = maskHeight;
                boundaryMask = new bool[size];

                for (int i = 0; i < boundaryMask.Length; i++)
                {
                    boundaryMask[i] = true;
                }

                if (oldMask != null && oldMask.Length > 0 && oldWidth > 0 && oldHeight > 0)
                {
                    int copyWidth = Mathf.Min(oldWidth, width);
                    int copyHeight = Mathf.Min(oldHeight, height);
                    for (int y = 0; y < copyHeight; y++)
                    {
                        for (int x = 0; x < copyWidth; x++)
                        {
                            int oldIndex = y * oldWidth + x;
                            int newIndex = y * width + x;
                            boundaryMask[newIndex] = oldMask[oldIndex];
                        }
                    }
                }

                maskWidth = width;
                maskHeight = height;
            }
        }

        private bool GetMaskCell(int x, int y)
        {
            int index = y * width + x;
            if (boundaryMask == null || index < 0 || index >= boundaryMask.Length)
            {
                return false;
            }

            return boundaryMask[index];
        }
    }
}

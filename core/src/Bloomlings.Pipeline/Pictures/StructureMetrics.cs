using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Pipeline.Pictures
{
    /// <summary>
    /// Structure metrics of a picture, computed on import (R7, T073):
    /// <list type="bullet">
    /// <item><c>regionCount</c>: 4-connected areas of one role;</item>
    /// <item><c>nestingDepth</c>: regions passed from a bottom-center entry to reach the deepest one (outer regions
    /// shield inner ones);</item>
    /// <item><c>backgroundShare</c>: the share of cells in background roles, in per mille.</item>
    /// </list>
    /// </summary>
    public static class StructureMetrics
    {
        public static PictureStructure Compute(BasePicture picture)
        {
            int w = picture.Width;
            int h = picture.Height;
            var component = new int[w * h];
            Array.Fill(component, -1);
            int regions = 0;
            var stack = new Stack<(int X, int Y)>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int role = picture.CellAt(x, y);
                    if (role < 0 || component[(y * w) + x] >= 0)
                    {
                        continue;
                    }

                    component[(y * w) + x] = regions;
                    stack.Push((x, y));
                    while (stack.Count > 0)
                    {
                        (int cx, int cy) = stack.Pop();
                        foreach ((int nx, int ny) in Neighbours(cx, cy, w, h))
                        {
                            if (component[(ny * w) + nx] < 0 && picture.CellAt(nx, ny) == role)
                            {
                                component[(ny * w) + nx] = regions;
                                stack.Push((nx, ny));
                            }
                        }
                    }

                    regions++;
                }
            }

            // 0-1 search from the bottom-center entry: entering another region costs 1.
            var cost = new int[w * h];
            Array.Fill(cost, int.MaxValue);
            var deque = new LinkedList<(int X, int Y)>();
            int ex = w / 2;
            if (picture.CellAt(ex, 0) != BasePicture.Stone)
            {
                cost[ex] = picture.CellAt(ex, 0) >= 0 ? 1 : 0;
                deque.AddLast((ex, 0));
            }

            while (deque.Count > 0)
            {
                (int x, int y) = deque.First!.Value;
                deque.RemoveFirst();
                int here = (y * w) + x;
                foreach ((int nx, int ny) in Neighbours(x, y, w, h))
                {
                    int value = picture.CellAt(nx, ny);
                    if (value == BasePicture.Stone)
                    {
                        continue;
                    }

                    int there = (ny * w) + nx;
                    bool step = value >= 0 && (picture.CellAt(x, y) < 0 || component[there] != component[here]);
                    int c = cost[here] + (step ? 1 : 0);
                    if (c < cost[there])
                    {
                        cost[there] = c;
                        if (step)
                        {
                            deque.AddLast((nx, ny));
                        }
                        else
                        {
                            deque.AddFirst((nx, ny));
                        }
                    }
                }
            }

            int depth = 1;
            int background = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int role = picture.CellAt(x, y);
                    if (role < 0)
                    {
                        continue;
                    }

                    if (cost[(y * w) + x] != int.MaxValue)
                    {
                        depth = Math.Max(depth, cost[(y * w) + x]);
                    }

                    if (picture.Roles[role].IsBackground)
                    {
                        background++;
                    }
                }
            }

            return new PictureStructure(Math.Max(1, regions), depth, background * 1000 / (w * h));
        }

        private static IEnumerable<(int X, int Y)> Neighbours(int x, int y, int w, int h)
        {
            if (y > 0)
            {
                yield return (x, y - 1);
            }

            if (x > 0)
            {
                yield return (x - 1, y);
            }

            if (x < w - 1)
            {
                yield return (x + 1, y);
            }

            if (y < h - 1)
            {
                yield return (x, y + 1);
            }
        }
    }
}

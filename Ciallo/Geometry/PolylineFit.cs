using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Geometry;

public static class PolylineFit
{
    /// <summary>
    /// Fit a poly-cubic Bézier curve to the given points using global least-squares.
    /// Uses arc-length parameterization with C1-continuous anchors.
    /// Sonnet 4.6 generated code
    /// </summary>
    /// <param name="points">Input points (assumed to be in sequential order).</param>
    /// <param name="nBezierPoints">Number of anchor points (≥ 2). 2 = single cubic segment, 3 = two segments.</param>
    /// <returns>Fitted BezierPoint anchors.</returns>
    public static BezierPoint[] FitBezier(this IReadOnlyList<Vector2> points, int nBezierPoints)
    {
        var pts = new List<Vector2>(points);
        int m = pts.Count;
        int n = Math.Max(nBezierPoints, 2);

        if (m == 0) return [];
        if (m == 1) return [new BezierPoint(pts[0], Vector2.Zero, Vector2.Zero)];

        // Arc-length parameterization → ts[i] ∈ [0, 1]
        var arcLen = new float[m];
        for (int i = 1; i < m; i++)
            arcLen[i] = arcLen[i - 1] + pts[i].DistanceTo(pts[i - 1]);
        float totalLen = arcLen[m - 1];
        var ts = new float[m];
        for (int i = 0; i < m; i++)
            ts[i] = totalLen > 1e-7f ? arcLen[i] / totalLen : (float)i / (m - 1);

        Vector2 a0 = pts[0], aLast = pts[m - 1];

        // --- Unknown index layout (per axis), total = 2*(n-1) ---
        // [0]         : Out[0]
        // [1 .. n-2]  : A[1] .. A[n-2]   (internal anchors, only when n > 2)
        // [n-1 .. 2n-4]: Out[1] .. Out[n-2] (internal out-handles, only when n > 2)
        // [2n-3]      : In[n-1]
        //
        // C1 continuity at internal anchors: In[k] = -Out[k]
        int numU = 2 * (n - 1);
        int idxInLast = 2 * n - 3;
        int IdxOut(int k) => k == 0 ? 0 : n - 2 + k; // Out[k] → index
        int IdxAnchor(int k) => k; // A[k] for k=1..n-2 → index k

        // Build design matrix (m × numU) and RHS vectors
        var mat = new float[m, numU];
        var rhsX = new float[m];
        var rhsY = new float[m];

        for (int i = 0; i < m; i++)
        {
            float gt = ts[i] * (n - 1);
            int k = Math.Clamp((int)MathF.Floor(gt), 0, n - 2);
            float u = gt - k;

            // Hermite-like cubic basis (endpoint-interpolating)
            float b0U = (1 - u) * (1 - u) * (1 + 2 * u);
            float b1U = u * u * (3 - 2 * u);
            float bOut = 3 * u * (1 - u) * (1 - u);
            float bIn = 3 * u * u * (1 - u);

            float rx = pts[i].X, ry = pts[i].Y;

            // A[k] contribution
            if (k == 0)
            {
                rx -= b0U * a0.X;
                ry -= b0U * a0.Y;
            }
            else mat[i, IdxAnchor(k)] += b0U;

            // A[k+1] contribution
            if (k + 1 == n - 1)
            {
                rx -= b1U * aLast.X;
                ry -= b1U * aLast.Y;
            }
            else mat[i, IdxAnchor(k + 1)] += b1U;

            // Out[k] contribution
            mat[i, IdxOut(k)] += bOut;

            // In[k+1] contribution: free for last anchor, else In = -Out (C1)
            if (k + 1 == n - 1)
                mat[i, idxInLast] += bIn;
            else
                mat[i, IdxOut(k + 1)] -= bIn;

            rhsX[i] = rx;
            rhsY[i] = ry;
        }

        // Normal equations: (matᵀ mat) x = matᵀ rhs
        var ata = new float[numU, numU];
        var atbX = new float[numU];
        var atbY = new float[numU];

        for (int j = 0; j < numU; j++)
        {
            for (int l = 0; l < numU; l++)
            {
                float sum = 0f;
                for (int i = 0; i < m; i++) sum += mat[i, j] * mat[i, l];
                ata[j, l] = sum;
            }
            float sx = 0f, sy = 0f;
            for (int i = 0; i < m; i++)
            {
                sx += mat[i, j] * rhsX[i];
                sy += mat[i, j] * rhsY[i];
            }
            atbX[j] = sx;
            atbY[j] = sy;
        }

        var solX = SolveLinear(ata, atbX, numU);
        var solY = SolveLinear(ata, atbY, numU);

        // Reconstruct anchor positions and handles
        var anchors = new Vector2[n];
        var outHandles = new Vector2[n];
        var inHandles = new Vector2[n];

        anchors[0] = a0;
        anchors[n - 1] = aLast;
        for (int j = 1; j < n - 1; j++)
            anchors[j] = new Vector2(solX[IdxAnchor(j)], solY[IdxAnchor(j)]);

        outHandles[0] = new Vector2(solX[0], solY[0]);
        inHandles[n - 1] = new Vector2(solX[idxInLast], solY[idxInLast]);

        for (int j = 1; j < n - 1; j++)
        {
            outHandles[j] = new Vector2(solX[IdxOut(j)], solY[IdxOut(j)]);
            inHandles[j] = -outHandles[j]; // C1 continuity
        }

        // Mirror end handles so endpoints also look smooth (no effect on segments)
        inHandles[0] = -outHandles[0];
        outHandles[n - 1] = -inHandles[n - 1];

        var curve = new BezierPoint[n];
        for (int j = 0; j < n; j++)
            curve[j] = new BezierPoint(anchors[j], inHandles[j], outHandles[j]);

        return curve;
    }

    // Gauss-Jordan elimination with partial pivoting. Solves a·x = b (non-destructive on a).
    private static float[] SolveLinear(float[,] a, float[] b, int n)
    {
        var aug = new float[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) aug[i, j] = a[i, j];
            aug[i, n] = b[i];
        }

        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
                if (MathF.Abs(aug[row, col]) > MathF.Abs(aug[pivot, col]))
                    pivot = row;
            for (int j = 0; j <= n; j++)
                (aug[col, j], aug[pivot, j]) = (aug[pivot, j], aug[col, j]);

            float diag = aug[col, col];
            if (MathF.Abs(diag) < 1e-10f) continue; // rank-deficient column → leave as zero

            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                float factor = aug[row, col] / diag;
                for (int j = col; j <= n; j++)
                    aug[row, j] -= factor * aug[col, j];
            }
        }

        var x = new float[n];
        for (int i = 0; i < n; i++)
            x[i] = MathF.Abs(aug[i, i]) > 1e-10f ? aug[i, n] / aug[i, i] : 0f;
        return x;
    }
}
public class SimplexNoise
{
    private int[] A = new int[3];
    private float s, u, v, w;
    private int i, j, k;
    private float onethird = 0.333333333f;
    private float onesixth = 0.166666667f;
    private int[] T;

    public SimplexNoise(int seed)
    {
        if (seed != 0)
        {
            Random rand = new Random(seed);
            for (int i = 0; i < 256; i++) T[i] = i;
            for (int i = 0; i < 256; i++)
            {
                int j = rand.Next(256);
                int temp = T[i];
                T[i] = T[j];
                T[j] = temp;
            }
        }
    }

    public double Evaluate(double x, double y)
    {
        s = (float)(x + y) * onethird;
        i = FastFloor(x + s);
        j = FastFloor(y + s);

        s = (i + j) * onesixth;
        u = (float)(x - i + s);
        v = (float)(y - j + s);

        A[0] = 0;
        A[1] = 0;
        A[2] = 0;

        int hi = u >= v ? 1 : 0;
        int lo = u < v ? 1 : 0;

        return K(hi) + K(3 - hi - lo) + K(lo) + K(0);
    }

    private int FastFloor(double x)
    {
        return x > 0 ? (int)x : (int)x - 1;
    }

    private float K(int a)
    {
        s = (A[0] + A[1] + A[2]) * onesixth;
        float x = u - A[0] + s;
        float y = v - A[1] + s;
        float z = w - A[2] + s;
        float t = 0.6f - x * x - y * y - z * z;
        int h = Shuffle(i + A[0], j + A[1], k + A[2]);
        A[a]++;
        if (t < 0) return 0;
        int b5 = h >> 5 & 1;
        int b4 = h >> 4 & 1;
        int b3 = h >> 3 & 1;
        int b2 = h >> 2 & 1;
        int b1 = h & 3;
        float p = b1 == 1 ? x : b1 == 2 ? y : z;
        float q = b1 == 1 ? y : b1 == 2 ? z : x;
        float r = b1 == 1 ? z : b1 == 2 ? x : y;
        p = b5 == b3 ? -p : p;
        q = b5 == b4 ? -q : q;
        r = b5 != (b4 ^ b3) ? -r : r;
        t *= t;
        return 8 * t * t * (p + (b1 == 0 ? q + r : b2 == 0 ? q : r));
    }

    private int Shuffle(int i, int j, int k)
    {
        return b(i, j, k, 0) + b(j, k, i, 1) + b(k, i, j, 2) + b(i, j, k, 3) +
               b(j, k, i, 4) + b(k, i, j, 5) + b(i, j, k, 6) + b(j, k, i, 7);
    }

    private int b(int i, int j, int k, int B)
    {
        return T[b(i, B) << 2 | b(j, B) << 1 | b(k, B)];
    }

    private int b(int N, int B)
    {
        return N >> B & 1;
    }
}
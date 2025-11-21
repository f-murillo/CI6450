using UnityEngine;

public static class TeletransportUtils
{
    public static Vector3 GetWrappedDirection(Vector3 from, Vector3 to)
    {
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, Camera.main.nearClipPlane));
        Vector3 topRight   = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, Camera.main.nearClipPlane));

        float width  = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        Vector3 delta = to - from;

        if (Mathf.Abs(delta.x) > width / 2f)
            delta.x -= Mathf.Sign(delta.x) * width;

        if (Mathf.Abs(delta.y) > height / 2f)
            delta.y -= Mathf.Sign(delta.y) * height;

        return delta;
    }

    public static Vector3 GetWrappedPosition(Vector3 position, float margin = 0f)
    {
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, Camera.main.nearClipPlane));
        Vector3 topRight   = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, Camera.main.nearClipPlane));

        float minX = bottomLeft.x - margin;
        float maxX = topRight.x + margin;
        float minY = bottomLeft.y - margin;
        float maxY = topRight.y + margin;

        Vector3 wrapped = position;

        if (wrapped.x < minX) wrapped.x = maxX;
        else if (wrapped.x > maxX) wrapped.x = minX;

        if (wrapped.y < minY) wrapped.y = maxY;
        else if (wrapped.y > maxY) wrapped.y = minY;

        return wrapped;
    }
}

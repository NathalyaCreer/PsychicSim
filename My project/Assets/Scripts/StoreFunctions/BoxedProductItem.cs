using UnityEngine;

public class BoxedProductItem : MonoBehaviour
{
    private ProductBox containingBox;
    private GameObject item;

    public void Initialize(ProductBox box, GameObject itemObject)
    {
        containingBox = box;
        item = itemObject;
    }

    public void TakeOutOfBox()
    {
        if (containingBox == null || item == null)
            return;

        containingBox.RemoveContainedProduct(item);
        containingBox = null;
    }
}
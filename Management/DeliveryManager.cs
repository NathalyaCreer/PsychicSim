using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Serializable]
    private class DeliveryOrder
    {
        public ProductData product;
        public int quantity;

        public DeliveryOrder(ProductData product, int quantity)
        {
            this.product = product;
            this.quantity = quantity;
        }
    }

    [Header("Day Cycle")]
    [SerializeField] private WorkShift workShift;

    [Header("Delivery Setup")]
    [SerializeField] private ProductBox deliveryBoxPrefab;
    [SerializeField] private Transform deliverySpawnPoint;
    [SerializeField] private float boxSpacing = 0.8f;
    [SerializeField, Min(1)] private int unitsPerDeliveryBox = 8;

    [Header("Truck Audio")]
    [SerializeField] private AudioSource deliveryAudioSource;
    [SerializeField] private AudioClip truckLeavingClip;

    private readonly List<DeliveryOrder> pendingOrders =
        new List<DeliveryOrder>();

    private bool deliveredThisDawn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (workShift == null)
        {
            Debug.LogError("DeliveryManager: Assign the WorkShift reference.", this);
            return;
        }

        workShift.PhaseChanged += HandlePhaseChanged;

        // WorkShift starts at Dawn, and its initial SetPhase(Dawn)
        // doesn't emit PhaseChanged because Dawn is already its phase.
        if (workShift.CurrentPhase == WorkShift.ShiftPhase.Dawn)
            DeliverPendingOrders();
    }

    private void OnDestroy()
    {
        if (workShift != null)
            workShift.PhaseChanged -= HandlePhaseChanged;

        if (Instance == this)
            Instance = null;
    }

    public bool QueuePurchase(ProductData product, int quantity)
    {
        if (product == null || quantity <= 0)
            return false;

        pendingOrders.Add(new DeliveryOrder(product, quantity));
        Debug.Log($"Delivery queued: {quantity} x {product.productName}.");
        return true;
    }

    private void HandlePhaseChanged(WorkShift.ShiftPhase phase)
    {
        if (phase == WorkShift.ShiftPhase.Dawn)
        {
            deliveredThisDawn = false;
            DeliverPendingOrders();
        }
    }

    public void DeliverPendingOrders()
    {
        if (deliveredThisDawn)
            return;

        if (deliveryBoxPrefab == null || deliverySpawnPoint == null)
        {
            Debug.LogError(
                "DeliveryManager: Assign Delivery Box Prefab and Delivery Spawn Point.",
                this
            );
            return;
        }

        int capacity = deliveryBoxPrefab.BoxCapacity;

        if (capacity < unitsPerDeliveryBox)
        {
            Debug.LogError(
                $"Delivery box needs at least {unitsPerDeliveryBox} box positions. " +
                $"It currently has {capacity}.",
                deliveryBoxPrefab
            );
            return;
        }

        if (pendingOrders.Count == 0)
        {
            deliveredThisDawn = true;
            Debug.Log("Dawn delivery: no orders waiting.");
            return;
        }

        int spawnedBoxIndex = 0;

        foreach (DeliveryOrder order in pendingOrders)
        {
            if (order.product == null || order.quantity <= 0)
                continue;

            // Order quantity is the number of boxes, not loose units.
            for (int i = 0; i < order.quantity; i++)
            {
                Vector3 spawnPosition =
                    deliverySpawnPoint.position +
                    deliverySpawnPoint.right * (boxSpacing * spawnedBoxIndex);

                ProductBox box = Instantiate(
                    deliveryBoxPrefab,
                    spawnPosition,
                    deliverySpawnPoint.rotation
                );

                box.InitializeDelivery(order.product, unitsPerDeliveryBox);

                spawnedBoxIndex++;
            }
        }

        pendingOrders.Clear();
        deliveredThisDawn = true;

        if (deliveryAudioSource != null && truckLeavingClip != null)
            deliveryAudioSource.PlayOneShot(truckLeavingClip);

        Debug.Log($"Dawn delivery complete. Spawned {spawnedBoxIndex} box(es).");
    }
}
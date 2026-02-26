Select * from Inventory.Vendor
Select * from Inventory.RateCard
Select * from Inventory.Purchaseorder
Select * from Inventory.PurchaseOrderItems
update Inventory.Vendor set IsApproved=1
update Inventory.Purchaseorder set IsApproved=1
update Inventory.RateCard set IsActive=1

CREATE VIEW Inventory.vw_PurchaseOrderDetails AS
SELECT 
    po.Id AS PurchaseOrderId,
    po.PONumber,
    po.PODateTime,
    po.BillingAddress,
    po.ShippingAddress,
    po.OfficeId,
    po.IsApproved,

    v.Id AS VendorId,
    v.Name AS VendorName,
    v.ContactPerson,
    v.ContactNumber,
    v.Email,

    poi.Id AS PurchaseOrderItemId,
    poi.ItemId,
    i.Name AS ItemName,
    poi.Quantity,
    poi.Rate,
    (poi.Quantity * poi.Rate) AS LineTotal

FROM 
    Inventory.PurchaseOrder po
INNER JOIN 
    Inventory.Vendor v ON po.VendorId = v.Id
INNER JOIN 
    Inventory.PurchaseOrderItems poi ON po.Id = poi.PurchaseOrderId
INNER JOIN 
    Inventory.Item i ON poi.ItemId = i.Id
WHERE 
    po.IsDeleted = 0
    AND po.IsApproved = 1
    AND v.IsActive = 1
    AND i.IsActive = 1;


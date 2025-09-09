using System.Collections.Generic;

namespace KeremProject1backend.Models.Responses
{
    public class CartItemResponse
    {
        public int ItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public string? ImageUrl { get; set; }
    }

    public class CartResponse
    {
        public int? UserId { get; set; } // Nullable - üye olmayan kullanıcılar için null
        public string? GuestId { get; set; } // Üye olmayan kullanıcılar için geçici ID
        public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
        public decimal TotalPrice { get; set; }
    }
} 
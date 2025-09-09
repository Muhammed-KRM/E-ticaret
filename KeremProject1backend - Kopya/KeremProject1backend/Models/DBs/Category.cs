using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeremProject1backend.Models.DBs
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // İsteğe bağlı: Kategori açıklaması
        public string? Description { get; set; }

        // Bu kategoriye ait ürünler için navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
} 
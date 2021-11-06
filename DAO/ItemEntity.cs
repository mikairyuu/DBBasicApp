namespace DBBasicApp.DAO
{
    public class ItemEntity
    {
        public int Id { get; set; }
        public string Sprite { get; set; }
        public int TypeId { get; set; }
        public int Price { get; set; }
        public bool OnSale { get; set; }
    }
}
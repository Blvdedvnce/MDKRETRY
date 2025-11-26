using System;
using System.Linq;
using System.Windows;

namespace _0202
{
    public partial class OrderWorks : Window
    {
        MDK0202Entities db = new MDK0202Entities();
        public int? EditingId;

        public OrderWorks()
        {
            InitializeComponent();

            // Загрузка типов партнёров
            TypePartner.ItemsSource = db.ТипПартнера_.ToList();
            TypePartner.DisplayMemberPath = "Наименование";
            TypePartner.SelectedValuePath = "Код";

            // Загрузка продукции
            ProductBox.ItemsSource = db.Продукция_.ToList();
            ProductBox.DisplayMemberPath = "Наименование_продукции";
            ProductBox.SelectedValuePath = "Код";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (EditingId.HasValue)
            {
                var partner = db.Партнеры_.FirstOrDefault(x => x.Код == EditingId.Value);
                if (partner != null)
                {
                    TypePartner.SelectedValue = partner.Тип_партнера;
                    PartnerName.Text = partner.Наименование_партнера;
                    DirectorFIO.Text = partner.Директор;
                    Address.Text = partner.Юридический_адрес_партнера;
                    Rating.Text = partner.Рейтинг?.ToString() ?? "0";
                    Phone.Text = partner.Телефон_партнера;
                    Email.Text = partner.Электронная_почта_партнера;
                    LoadCart(partner.Код);
                }
            }
        }

        // Добавление продукта
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProductBox.SelectedItem is Продукция_ product))
            {
                MessageBox.Show("Выберите продукцию!");
                return;
            }

            if (!int.TryParse(QtyBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Количество должно быть больше нуля!");
                return;
            }

            if (!EditingId.HasValue)
            {
                MessageBox.Show("Сначала сохраните партнёра!");
                return;
            }

            // Проверка есть ли уже такой продукт в корзине
            var existing = db.ПродуктыПартнера_.FirstOrDefault(c => c.КодПартнера == EditingId.Value && c.КодПродукции == product.Код);
            if (existing != null)
            {
                existing.Количество_продукции += qty;
            }
            else
            {
                var cartItem = new ПродуктыПартнера_
                {
                    КодПартнера = EditingId.Value,
                    КодПродукции = product.Код,
                    Количество_продукции = qty
                };
                db.ПродуктыПартнера_.Add(cartItem);
            }

            db.SaveChanges();

            LoadCart(EditingId.Value);
        }

        // Загрузка корзину
        private void LoadCart(int partnerId)
        {
            CartList.Items.Clear();
            double total = 0;

            // Берём все товары из корзины для текущего партнёра
            var cartItems = db.ПродуктыПартнера_.Where(c => c.КодПартнера == partnerId).ToList();

            foreach (var c in cartItems)
            {
                // Берём саму продукцию
                var product = db.Продукция_.FirstOrDefault(p => p.Код == c.КодПродукции);
                if (product == null) continue;

                // Считаем сумму
                double sum = (double)(c.Количество_продукции * product.Минимальная_стоимость_для_партнера);
                total += sum;
                CartList.Items.Add($"{product.Наименование_продукции} — {c.Количество_продукции} шт. — {sum} руб.");
            }
            TotalPrice.Content = $"Итог: {total} руб.";
        }

        // Сохранение партнёра
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (TypePartner.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип партнера!");
                return;
            }

            if (string.IsNullOrWhiteSpace(PartnerName.Text))
            {
                MessageBox.Show("Введите наименование партнера!");
                return;
            }

            if (!int.TryParse(Rating.Text, out int rating) || rating < 0)
            {
                MessageBox.Show("Рейтинг должен быть целым неотрицательным числом!");
                return;
            }

            if (!Phone.Text.All(c => char.IsDigit(c) || c == ' '))
            {
                MessageBox.Show("Телефон может содержать только цифры и пробелы!");
                return;
            }

            if (string.IsNullOrWhiteSpace(Email.Text) || !System.Text.RegularExpressions.Regex.IsMatch(Email.Text, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"))
            {
                MessageBox.Show("Некорректный формат Email!");
                return;
            }

            Партнеры_ partner;

            if (EditingId.HasValue)
            {
                partner = db.Партнеры_.First(x => x.Код == EditingId.Value);
            }
            else
            {
                partner = new Партнеры_();
                db.Партнеры_.Add(partner);
            }

            partner.Тип_партнера = (int)TypePartner.SelectedValue;
            partner.Наименование_партнера = PartnerName.Text.Trim();
            partner.Директор = DirectorFIO.Text.Trim();
            partner.Юридический_адрес_партнера = Address.Text.Trim();
            partner.Рейтинг = rating;
            partner.Телефон_партнера = Phone.Text.Trim();
            partner.Электронная_почта_партнера = Email.Text.Trim();

            db.SaveChanges();

            if (!EditingId.HasValue)
            {
                EditingId = partner.Код;
            }

            MessageBox.Show("Партнёр сохранён!");
            LoadCart(partner.Код);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow back = new MainWindow();
            back.Show();
            this.Close();
        }
    }
}

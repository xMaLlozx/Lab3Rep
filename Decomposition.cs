using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Kurs_2
{
    public interface Encryptor
    {
        string Encrypt(string data);
        string Encrypt(string data, string path);
        string Decrypt(string data);
        string Decrypt(string data, string path);
    }
    /// <summary>
    /// Шаблонная фабрика
    /// </summary>
    public class EncryptionFactory
    {
        public static Encryptor CreateEncryptor(int algorithm, object key)
        {
            switch (algorithm)
            {
                case 0:
                    return new Caesar(Convert.ToInt32(key));
                case 1:
                    return new Atbash((string)key);
                default:
                    throw new ArgumentException("Неизвестный алгоритм шифрования");
            }
        }
    }
    public static class CipherUtils
    {
        public static char ShiftCharacter(char el, int shift, string alphabet)
        {
            if (alphabet.Contains(el))
            {
                int index = (alphabet.IndexOf(el) + shift) % alphabet.Length;
                if (index < 0) index += alphabet.Length; // Handle negative shifts
                return alphabet[index];
            }
            return el;
        }
    }
    /// <summary>
    /// Шифр Цезаря
    /// </summary>
    public class Caesar : Encryptor
    {
        private int key = 0;
        private string additionalCharacters;

        public Caesar(int key, EncryptionConfig config)
        {
            this.key = key;
            this.additionalCharacters = config.AdditionalCharacters;
        }

        public string Encrypt(string data)
        {
            return ProcessData(data, key);
        }

        public string Decrypt(string data)
        {
            return ProcessData(data, -key);
        }

        private string ProcessData(string data, int shift)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string extendedAlphabet = alphabet + additionalCharacters;

            // Декомпозиция условного оператора
            return new string(data.ToUpper().Select(el =>
                CipherUtils.ShiftCharacter(el, shift, extendedAlphabet)).ToArray()).ToLower();
        }
        public string Encrypt(string data, string path)
        {
            string res = Encrypt(data) + "~"; // Знак ~ является концом строки, которое добавлено в BMP файл, чтобы знать, когда остановиться программе
            byte[] bmpBytes = File.ReadAllBytes(path);
            if (bmpBytes[0] != 'B' || bmpBytes[1] != 'M') MessageBox.Show("Файл не является BMP изображением.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);

            int pixelDataOffset = BitConverter.ToInt32(bmpBytes, 10);
            byte[] resBytes = Encoding.UTF8.GetBytes(res);
            int resIndex = 0;
            int bitIndex = 0;

            for (int i = pixelDataOffset; i < bmpBytes.Length; i++)
            {
                // Внедряем только если данные ещё остались
                if (resIndex < resBytes.Length)
                {
                    // Заменяем последний бит текущего байта
                    bmpBytes[i] = (byte)((bmpBytes[i] & ~1) | ((resBytes[resIndex] >> (7 - bitIndex)) & 1));

                    bitIndex++;
                    if (bitIndex == 8) // Переходим к следующему байту данных
                    {
                        bitIndex = 0;
                        resIndex++;
                    }
                }
                else break; // Если данные закончились, выходим из цикла
            }

            File.WriteAllBytes(path, bmpBytes);
            return "Успешно!";
        }
        public string Decrypt(string data, string path)
        {
            // Чтение файла BMP
            string res = "";
            byte[] bmpBytes = File.ReadAllBytes(path);
            if (bmpBytes[0] != 'B' || bmpBytes[1] != 'M') MessageBox.Show("Файл не является BMP изображением.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);

            int pixelDataOffset = BitConverter.ToInt32(bmpBytes, 10);
            List<byte> extractedBytes = new List<byte>();
            int bitIndex = 0;
            byte currentByte = 0;

            for (int i = pixelDataOffset; i < bmpBytes.Length; i++)
            {
                // Извлечение младшего бита текущего байта
                int lsb = bmpBytes[i] & 1;

                // Добавляем этот бит в текущий собираемый байт
                currentByte = (byte)(currentByte * 2 + lsb);
                bitIndex++;

                // Если собрали 8 бит, добавляем байт в список
                if (bitIndex == 8)
                {
                    extractedBytes.Add(currentByte);
                    bitIndex = 0;
                    currentByte = 0;

                    // Если встретили два нулевых байта подряд, выходим
                    if (extractedBytes[extractedBytes.Count - 1] == '~') 
                        break;
                }
            }
            if (extractedBytes.Count > 0 && extractedBytes[extractedBytes.Count - 1] == '~')
                extractedBytes.RemoveAt(extractedBytes.Count - 1);

            return res = Decrypt(Encoding.UTF8.GetString(extractedBytes.ToArray()).TrimEnd('\0'));
        }
    }
    public class EncryptionConfig
    {
        public string AdditionalCharacters { get; set; } = "";
    }
    /// <summary>
    /// Шифр Атбаша
    /// </summary>
    public class Atbash : Encryptor
    {
        private string key = "";
        public Atbash(string key) { this.key = key; }

        public string Encrypt(string data)
        {
            return data + "Atbash";
        }
        public string Encrypt(string data, string path)
        {
            return "Успешно!";
        }
        public string Decrypt(string data)
        {
            return data + "Atbash";
        }
        public string Decrypt(string data, string path)
        {
            return data + "Atbash";
        }
    }
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string path;
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Шифрование текста
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Radiobutton_Shifr.IsChecked != true && RadioButton_Steg.IsChecked != true) { MessageBox.Show("Выберите действие\nШифрование - Стеганография", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning); return; };
            if (ComboBox1.SelectedIndex == 0)
            {
                switch (Radiobutton_Shifr.IsChecked)
                {
                    case true:
                        {
                            Encryptor encry = EncryptionFactory.CreateEncryptor(0, textBoxKey.Text);
                            textBox_res.Text = encry.Encrypt(Client_text.Text);
                            return;
                        }
                    case false:
                        {
                            Encryptor encry = EncryptionFactory.CreateEncryptor(0, textBoxKey.Text);
                            textBox_res.Text = encry.Encrypt(Client_text.Text, path);
                            return;
                        }
                }
            }
            else if(ComboBox1.SelectedIndex == 1)
            {
                switch (Radiobutton_Shifr.IsChecked)
                {
                    case true:
                        {
                            Encryptor encry = EncryptionFactory.CreateEncryptor(1, textBoxKey.Text);
                            textBox_res.Text = encry.Encrypt(Client_text.Text);
                            return;
                        }
                    case false:
                        {
                            Encryptor encry = EncryptionFactory.CreateEncryptor(1, textBoxKey.Text);
                            textBox_res.Text = encry.Encrypt(Client_text.Text, textBlockImage.Text);
                            return;
                        }
                }
            }
        }
        /// <summary>
        /// Дешифрование текста
        /// </summary>
        private void Button_Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (Radiobutton_Shifr.IsChecked != true && RadioButton_Steg.IsChecked != true) { MessageBox.Show("Выберите действие\nШифрование - Стеганография", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning); return; };
            if (ComboBox1.SelectedIndex == 0)
            {
                    switch (Radiobutton_Shifr.IsChecked)
                    {
                        case true:
                            {
                                Encryptor encry = EncryptionFactory.CreateEncryptor(0, textBoxKey.Text);
                                textBox_res.Text = encry.Decrypt(Client_text.Text);
                                return;
                            }
                        case false:
                            {
                                Encryptor encry = EncryptionFactory.CreateEncryptor(0, textBoxKey.Text);
                                textBox_res.Text = encry.Decrypt(Client_text.Text, path);
                                return;
                            }
                    }
            }
            else if (ComboBox1.SelectedIndex == 1)
            {
                Encryptor encryptor = EncryptionFactory.CreateEncryptor(1, textBoxKey.Text);
                textBox_res.Text = encryptor.Decrypt(Client_text.Text);
            }
        }
        /// <summary>
        /// Выключение видимости текста внутри BorderImage
        /// </summary>
        private void BorderImage_MouseEnter(object sender, MouseEventArgs e)
        {
            textBlockImage.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Включение видимости текста внутри BorderImage
        /// </summary>
        private void BorderImage_MouseLeave(object sender, MouseEventArgs e)
        {
            textBlockImage.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Редактирование при вводе ключа
        /// </summary>
        private void textBoxKey_MouseEnter(object sender, MouseEventArgs e)
        {
            if(textBoxKey.Text == "Введите ключ" || textBoxKey.Text == "")
            {
                textBoxKey.Text = "";
                textBoxKey.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
            else
                textBoxKey.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        }
        /// <summary>
        /// Выключение видимости BorderImage для работы с ним
        /// </summary>
        private void RadioButtonShifr_Checked(object sender, RoutedEventArgs e)
        {
            BorderImage.Visibility = Visibility.Collapsed;
            buttonAddImage.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// Включение видимости BorderImage для работы с ним
        /// </summary>
        private void RadioButtonStegan_Checked(object sender, RoutedEventArgs e)
        {
            BorderImage.Visibility= Visibility.Visible;
            buttonAddImage.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Включение textbox при входе в объект
        /// </summary>
        private void Border_key_MouseMove(object sender, MouseEventArgs e)
        {
            textBoxKey.IsEnabled = true;
            if (textBoxKey.Text == "Введите ключ" || textBoxKey.Text == "")
            {
                textBoxKey.Text = "";
                textBoxKey.Background = new SolidColorBrush(Color.FromRgb(247, 247, 247));
            }
        }
        /// <summary>
        /// Отключение textbox при выходе за объект
        /// </summary>
        private void Border_key_MouseLeave(object sender, MouseEventArgs e)
        {
            if (textBoxKey.Text != "")
            {
                textBoxKey.IsEnabled = false;
                textBoxKey.Background = Brushes.White;
            }
            else
            {
                textBoxKey.IsEnabled = false;
                textBoxKey.Text = "Введите ключ";
                textBoxKey.Background = Brushes.White;
            }
        }
        /// <summary>
        /// Редактирование неправильно вводимых ключей, для каждого шифра с своим ключом
        /// </summary>
        private void textBoxKey_TextInput(object sender, TextChangedEventArgs e)
        {
            string text = textBoxKey.Text;
            if (ComboBox1.SelectedIndex == 0)
                if (int.TryParse(textBoxKey.Text, out _) == false)
                    if (textBoxKey.Text.Length != 0 && textBoxKey.Text != "Введите ключ")
                    {
                        textBoxKey.Text = text.Remove(textBoxKey.Text.Length - 1, 1);
                        textBoxKey.CaretIndex = textBoxKey.Text.Length;
                    }
        }
        /// <summary>
        /// Кнопка добавления фотки
        /// </summary>
        private void buttonAddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Image files|*.jpg;*.bmp;*.png" ;
            if (ofd.ShowDialog() != true) return;

            path = ofd.FileName;
        }
    }
}
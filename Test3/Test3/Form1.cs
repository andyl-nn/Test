using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;

//using System.Text.Json;

namespace Test3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "Загрузите информацию о типах данных";
        }

        private int stage = 1;                  // исходное состояние системы
        TypeDatas[] typeData;                   // структура для загружаемых типов данных
        XDocument xmldoc = new XDocument();     // создаем xml документ

        private void button1_Click(object sender, EventArgs e)
        {
            if (stage == 1)     // первая стадия: загружаем структуру типов данных
            {
                // открываем экран для вывода информации
                listBox1.Visible = false;       // очищаем лист и скрываем элемент формы
                textBox1.Text = "";
                textBox1.Visible = true;        // переключаемся на отображение текста

                openFileDialog1.Filter = "Json files(*.json)|*.json|All files(*.*)|*.*";
                openFileDialog1.Title = "Загрузите файл типов данных (*.json)";
                openFileDialog1.FileName = "";

                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;
                string fileName = openFileDialog1.FileName;             // открываем выбранный файл
                string fileText;
                    using (StreamReader fs = new StreamReader(fileName))
                    {
                        fileText = fs.ReadToEnd();                      // прочитали весь файл в строку текста
                    }
                // первая раскладка файла json на составляющие
                string[] items = fileText.Split(new char[] { '{' }, StringSplitOptions.RemoveEmptyEntries);
                int numTypes = 0;
                foreach (string it in items)            // определяем количество блоков описаний типов данных
                {
                      if (it.Contains("TypeName")) numTypes++;
                }
                if(numTypes == 0)
                {
                    textBox1.Text = "ОШИБКА Файл не содержит описаний типов даных";
                    return;
                }

                textBox1.Text = "Файл считан успешно\r\n\r\nЗагружены описания для следующих типов данных:\r\n";

                typeData = new TypeDatas[numTypes];     // создаем структуру описывающую типы данных

                // заполняем массив структур типов данных
                numTypes = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Contains("TypeName"))
                    {
                        string[] pole = items[i++].Split(new char[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
                        string typeName = pole[1].Trim(new char[] { ' ', '}', '{', '"', ',', '\n' });
                        textBox1.Text += "\r\n Тип " + (numTypes + 1) + " - " + typeName;    // выводим имена считанных типов данных
                        // разбираем следующую за именем типа строку
                        string dataStr = items[i].Trim(new char[] { ' ', '}', '{', '[', ']', ',', '\n' });
                        string[] data = dataStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        typeData[numTypes] = new TypeDatas(typeName, data.Length);

                        for (int j = 0; j < data.Length; j++)               // очищаем и разбираем на элементы
                        {
                             data[j] = data[j].Trim(new char[] { ' ', '}', '{', '[', ']', ',', '\n' });
                             string[] itemName = data[j].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                             // заносим данные в структуру
                             typeData[numTypes].propertys[j].name = itemName[0].Trim(new char[] { '"', ' ', '}' });
                             typeData[numTypes].propertys[j].type = itemName[1].Trim(new char[] { '"', ' ', '}' });
                             typeData[numTypes].propertys[j].SetSize();     // устанавливаем размер поля данных в байтах
                        }
                        numTypes++;
                    }
                }
                stage = 2;     // переходим к стадии 2
                this.Text = "Загрузите исходные данные для привязки";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (stage == 2)     // вторая стадия: считываем исходные данные и формируем выходные
            {
                textBox1.Text = "";         // очищаем и скрываем элемент формы
                textBox1.Visible = false;
                listBox1.Items.Clear();
                listBox1.Visible = true;    // переключаемся на отображение списка

                openFileDialog1.Filter = "Data files(*.csv)|*.csv|All files(*.*)|*.*";
                openFileDialog1.Title = "Загрузите файл исходных данных";
                openFileDialog1.FileName = "";

                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;
                string fileName = openFileDialog1.FileName;             // открываем выбранный файл
                string fileText;
                using (StreamReader fs = new StreamReader(fileName))
                {
                    fileText = fs.ReadToEnd();                          // прочитали весь файл в строку текста
                }

                string[] inputData = fileText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                listBox1.Items.AddRange(inputData);                     // выводим на экран список тегов для выбора (двойной клик на строке)
                this.Text = "Выберите строку данных для привязки (двойной клик)";

                stage = 3;     // переходим к стадии 3
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (stage == 3)     // третья стадия: сохранение выходных данных
            {
                saveFileDialog1.Filter = "Data files(*.xml)|*.xml|All files(*.*)|*.*";
                saveFileDialog1.Title = "Сохраните файл данных";
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

                string fileName = saveFileDialog1.FileName;         // открываем файл для сохранения
                using (StreamWriter fs = new StreamWriter(fileName, false))
                {

                    fs.Write(xmldoc);                               // записываем xml документ в файл
                }
                MessageBox.Show("Все удачно, файл сохранен");

                // процесс полностью завершен, выходим из приложения
                Application.Exit();
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // обрабатываем выбранный тег из набора входных данных
            if (listBox1.SelectedIndex >= 0)                    // если есть выбранный элемент списка
            {
                string selString = listBox1.SelectedItem.ToString();
                string[] dataTeg = selString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (!dataTeg[0].Contains("root")) return;       // проверка на правильность данных

                int selType = -1;
                for (int i = 0; i < typeData.Length; i++)
                {
                    if (Equals(typeData[i].typeName, dataTeg[1])) selType = i;
                }
                if (selType < 0)
                {
                    MessageBox.Show("Несуществующий тип данных", "ОШИБКА");
                    return;
                }

                listBox1.Items.Clear();         // очищаем лист и скрываем элемент формы
                listBox1.Visible = false;

                // ***** можем приступать к выводу данных ******
                textBox1.Text = "";
                textBox1.Visible = true;        // открываем экран для вывода сформированного xml

                int address = 0;

                // создаем корневой элемент
                XElement root = new XElement("root");
                XAttribute BindingAttr;
                XElement NodePathElem;
                XElement AddressElem;

                for(int i = 0; i < typeData[selType].propertys.Length; i++)
                {
                    // создаем элемент
                    XElement item = new XElement("item");
                    BindingAttr = new XAttribute("Binding", "Introduced");
                    NodePathElem = new XElement("node-path", dataTeg[0] + '.' + typeData[selType].propertys[i].name);
                    AddressElem = new XElement("address", address);
                    address += typeData[selType].propertys[i].size;
                    item.Add(BindingAttr);
                    item.Add(NodePathElem);
                    item.Add(AddressElem);
                    root.Add(item);                     // добавляем элемент в корневой элемент
                }

                xmldoc.Add(root);                       // добавляем корневой элемент в документ
                textBox1.Text = xmldoc.ToString();      // распечатываем для контроля
                this.Text = "Данные сформированы";
            }
        }
    }

    struct TypeDatas    // структура по типу набора данных
    {
        public string typeName;                     // имя типа набора данных
        public ItemPropertys[] propertys;           // массив параметров данных

        public TypeDatas(string name, int sizeProp)
        {
            this.typeName = name;
            this.propertys = new ItemPropertys[sizeProp];
        }
    }

    struct ItemPropertys // свойства параметра: имя, тип данных, размер поля
    {
        public string name;
        public string type;
        public int size;

        public void SetSize()           // расчет размера поля в байтах по типу данных
        {
            switch (this.type)
            {
                case "bool":
                    this.size = 1;
                    break;
                case "int":
                    this.size = 4;
                    break;
                case "double":
                    this.size = 8;
                    break;
                default:
                    this.size = 0;      // ситуация ошибки, неопределенный тип данных
                    break;
            }
        }
    }
}

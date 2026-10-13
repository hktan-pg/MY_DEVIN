using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyApp.App.Models;

namespace MyApp.App
{
    public partial class CategoryDialog : Window
    {
        public Category Category { get; private set; }
        public bool IsSaved { get; private set; } = false;
        private List<Category> _availableParentCategories;
        private int? _originalCategoryId;

        public CategoryDialog(Category? category = null, List<Category>? availableParentCategories = null)
        {
            InitializeComponent();

            // Unlock dark title bar to match the black theme
            WindowThemeHelper.EnableDarkTitleBar(this);

            _availableParentCategories = availableParentCategories ?? new List<Category>();
            
            if (category != null)
            {
                // Work with a copy so cancel doesn't mutate the original
                _originalCategoryId = category.Id;
                Category = new Category
                {
                    Id = category.Id,
                    Name = category.Name,
                    CategoryType = category.CategoryType,
                    ParentId = category.ParentId,
                    IsActive = category.IsActive
                };
                Title = "Edit Category";
            }
            else
            {
                Category = new Category
                {
                    IsActive = true,
                    CategoryType = "Expense"
                };
                Title = "Add Category";
            }

            DataContext = Category;
            
            // Load parent categories based on the current type
            LoadParentCategories(_originalCategoryId);
        }

        private void LoadParentCategories(int? excludeId)
        {
            var filteredCategories = _availableParentCategories
                .Where(c => c.Id != excludeId && c.CategoryType == Category.CategoryType)
                .ToList();
            
            ParentCategoryComboBox.ItemsSource = filteredCategories;
            ParentCategoryComboBox.SelectedIndex = -1;
            
            if (Category.ParentId.HasValue)
            {
                ParentCategoryComboBox.SelectedValue = Category.ParentId.Value;
            }
        }

        private void CategoryTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Category == null || ParentCategoryComboBox == null) return;

            // Clear the parent selection and reload parent categories for the new type
            Category.ParentId = null;
            LoadParentCategories(_originalCategoryId);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Category.Name))
            {
                System.Windows.MessageBox.Show("Name is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Category.CategoryType))
            {
                System.Windows.MessageBox.Show("Category Type is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
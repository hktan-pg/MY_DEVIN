using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    public class CategoriesViewModel : ViewModelBase
    {
        private readonly CategoryService _categoryService;
        private ObservableCollection<Category> _incomeCategories;
        private ObservableCollection<Category> _expenseCategories;
        private Category? _selectedCategory;

        public CategoriesViewModel(CategoryService categoryService)
        {
            _categoryService = categoryService;
            _incomeCategories = new ObservableCollection<Category>();
            _expenseCategories = new ObservableCollection<Category>();
            
            LoadCategoriesCommand = new RelayCommand(async _ => await LoadCategoriesAsync());
            AddCategoryCommand = new RelayCommand(_ => AddCategory());
            EditCategoryCommand = new RelayCommand(_ => EditCategory(), _ => CanEditCategory());
            DeactivateCategoryCommand = new RelayCommand(async _ => await DeactivateCategoryAsync(), _ => CanEditCategory());
            DeleteCategoryCommand = new RelayCommand(async _ => await DeleteCategoryAsync(), _ => CanEditCategory());
        }

        public ObservableCollection<Category> IncomeCategories
        {
            get => _incomeCategories;
            set => SetProperty(ref _incomeCategories, value);
        }

        public ObservableCollection<Category> ExpenseCategories
        {
            get => _expenseCategories;
            set => SetProperty(ref _expenseCategories, value);
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                ((RelayCommand)EditCategoryCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeactivateCategoryCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteCategoryCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand LoadCategoriesCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeactivateCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }

        public async Task LoadCategoriesAsync()
        {
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            
            IncomeCategories = new ObservableCollection<Category>(
                allCategories.Where(c => c.CategoryType == "Income"));
            
            ExpenseCategories = new ObservableCollection<Category>(
                allCategories.Where(c => c.CategoryType == "Expense"));
        }

        private async void AddCategory()
        {
            var allCategories = IncomeCategories.Concat(ExpenseCategories).ToList();
            var dialog = new CategoryDialog(null, allCategories);
            
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var category = dialog.Category;

                // Check for duplicate name within the same type
                var nameExists = await _categoryService.CategoryNameExistsAsync(category.Name, category.CategoryType);
                if (nameExists)
                {
                    System.Windows.MessageBox.Show(
                        $"A category named '{category.Name}' already exists for {category.CategoryType}.",
                        "Duplicate Category",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Save to database
                await _categoryService.CreateCategoryAsync(category);
                await LoadCategoriesAsync();
            }
        }

        private bool CanEditCategory()
        {
            return SelectedCategory != null;
        }

        private async void EditCategory()
        {
            if (SelectedCategory == null) return;

            var allCategories = IncomeCategories.Concat(ExpenseCategories).ToList();
            var dialog = new CategoryDialog(SelectedCategory, allCategories);
            
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var edited = dialog.Category;

                // Check for duplicate name within the same type (excluding self)
                var nameExists = await _categoryService.CategoryNameExistsAsync(edited.Name, edited.CategoryType, edited.Id);
                if (nameExists)
                {
                    System.Windows.MessageBox.Show(
                        $"A category named '{edited.Name}' already exists for {edited.CategoryType}.",
                        "Duplicate Category",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Update the selected category with the edited values
                SelectedCategory.Name = edited.Name;
                SelectedCategory.CategoryType = edited.CategoryType;
                SelectedCategory.ParentId = edited.ParentId;
                SelectedCategory.IsActive = edited.IsActive;

                // Update in database
                await _categoryService.UpdateCategoryAsync(SelectedCategory);
                await LoadCategoriesAsync();
            }
        }

        private async Task DeactivateCategoryAsync()
        {
            if (SelectedCategory == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to deactivate '{SelectedCategory.Name}'?",
                "Confirm Deactivation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _categoryService.DeactivateCategoryAsync(SelectedCategory.Id);
                SelectedCategory.IsActive = false;
                
                // Refresh the list
                await LoadCategoriesAsync();
            }
        }

        private async Task DeleteCategoryAsync()
        {
            var category = SelectedCategory;
            if (category == null) return;

            // Check if category has transaction history
            var hasHistory = await _categoryService.HasTransactionHistoryAsync(category.Id);
            
            if (hasHistory)
            {
                System.Windows.MessageBox.Show(
                    "This category has transaction history and cannot be deleted. Please deactivate it instead.",
                    "Cannot Delete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to permanently delete '{category.Name}'?",
                "Confirm Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _categoryService.DeleteCategoryAsync(category.Id);
                if (success)
                {
                    if (category.CategoryType == "Income")
                    {
                        IncomeCategories.Remove(category);
                    }
                    else
                    {
                        ExpenseCategories.Remove(category);
                    }
                    SelectedCategory = null;
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to delete the category. It may have child categories.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}
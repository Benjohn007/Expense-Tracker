using Expense_Tracker.Context;
using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-30);
            DateTime EndDate = DateTime.Today;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = string.Format(culture, "{0:C0}", TotalIncome);
            /*ViewBag.TotalIncome = TotalIncome.ToString("C0");*/


            //Total Expense
            int TotalExpense = SelectedTransactions
              .Where(i => i.Category.Type == "Expense")
              .Sum(j => j.Amount);
            ViewBag.TotalExpense = string.Format(culture, "{0:C0}", TotalExpense);
            /*ViewBag.TotalExpense = TotalExpense.ToString("C0");*/

            //Balance
            int Balance = TotalIncome - TotalExpense;
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = string.Format(culture, "{0:C0}", Balance);
            /*ViewBag.Balance = Balance.ToString("C0");*/


            //Doughnut Chart - Expense By category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    //formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                    formattedAmount = string.Format(culture, "{0:C0}", k.Sum(j => j.Amount))

                })
                .OrderByDescending(l=>l.amount)
                .ToList();


            //spline chart - income vs expense

            //income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(y => y.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type =="Expense")
                .GroupBy(y => y.Date)
                .Select (k => new SplineChartData()
                {
                    day= k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last30Days = Enumerable.Range(0, 30)
                .Select(x => StartDate.AddDays(x).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplinechartData = from day in Last30Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Category)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}

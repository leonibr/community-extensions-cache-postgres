# Performance Testing in CI/CD

This document explains how performance testing is integrated into our GitHub Actions CI/CD pipeline for the PostgreSQL distributed cache library.

## Overview

We use a **hybrid approach** with three complementary workflows plus **historical trend analysis**:

1. **Scheduled Monitoring** - Regular performance tracking with historical storage
2. **PR Validation** - Optional performance testing with regression detection
3. **Release Validation** - Comprehensive testing before releases
4. **Interactive Dashboard** - Historical trends and regression analysis

## 📊 Performance Dashboard

### Live Dashboard

Once configured, your performance dashboard will be available at:
**`https://leonibr.github.io/community-extensions-cache-postgres/benchmarks/`**

### Features

- **📈 Historical Trends**: Interactive charts showing performance over time
- **⚡ Regression Detection**: Automatic alerts when performance degrades >20% (PRs) or >50% (scheduled)
- **🔍 Drill-down Analysis**: Click data points to see specific commit details
- **🎯 Multiple Metrics**: Time, memory allocation, and percentile tracking

## Workflows

### 1. Scheduled Performance Benchmarks

**File:** `.github/workflows/benchmarks-scheduled.yml`

**Triggers:**

- **Schedule:** Monday and Thursday at 2 AM UTC
- **Manual:** Via workflow dispatch in GitHub Actions UI
- **Automatic:** When code changes in `Extensions.Caching.PostgreSql/` or `Benchmarks/`

**What it does:**

- Runs core operations benchmark by default
- Can run specific benchmarks or full suite via manual trigger
- **Stores results in historical database**
- **Updates live dashboard automatically**
- Creates GitHub job summaries with performance data
- **Triggers alerts on >50% performance degradation**

**Usage:**

```bash
# Runs automatically, or trigger manually from GitHub Actions UI
# Select which benchmarks to run: core, datasize, expiration, concurrency, bulk, or all
```

### 2. PR Performance Validation

**File:** `.github/workflows/benchmarks-pr.yml`

**Triggers:**

- When PR touches performance-sensitive code
- Only runs when explicitly requested

**How to trigger:**

1. **Option 1:** Add `performance` label to your PR
2. **Option 2:** Include `[perf]` in your PR title

**What it does:**

- Runs core operations benchmark (fastest)
- **Compares results against main branch baseline**
- **Triggers alerts on >20% performance degradation**
- Posts results with historical context as PR comment
- Provides guidance for additional testing
- Stores detailed results for 14 days

**Example PR titles:**

- `[perf] Optimize connection pooling`
- `Fix caching logic [perf]`

### 3. Release Performance Validation

**File:** `.github/workflows/benchmarks-release.yml`

**Triggers:**

- **Automatic:** When a release is published
- **Manual:** Via workflow dispatch

**What it does:**

- Runs comprehensive benchmark suite (all 5 benchmark types)
- **Updates historical dashboard with release data**
- Generates detailed performance report
- Updates release notes with performance summary
- Creates performance review issue
- Stores results for 1 year
- Performs basic regression analysis

## Understanding Results

### Historical Trend Analysis

The dashboard provides several views for performance analysis:

**📈 Trend Charts:**

- Performance over time with commit correlation
- Multiple metric visualization (time, memory, percentiles)

**🔍 Regression Detection:**

- Automatic highlighting of performance degradations
- Stablished thresholds (20% for PRs, 50% for scheduled runs)
- Commit-level correlation for root cause analysis

**📊 Comparative Analysis:**

- Baseline comparisons for PR validation
- Release-to-release performance tracking
- Cross-benchmark correlation analysis

### Benchmark Types

| Benchmark     | Purpose                                      | Typical Runtime | Dashboard Chart      |
| ------------- | -------------------------------------------- | --------------- | -------------------- |
| `core`        | Basic operations (Get, Set, Delete, Refresh) | ~5-10 minutes   | Real-time trends     |
| `datasize`    | Performance with 1KB to 1MB payloads         | ~10-15 minutes  | Size impact analysis |
| `expiration`  | Different expiration strategies              | ~10-15 minutes  | Strategy comparison  |
| `concurrency` | 2-16 concurrent operations                   | ~15-20 minutes  | Scalability trends   |
| `bulk`        | Batch operations (10-500 items)              | ~15-25 minutes  | Throughput analysis  |

### Key Metrics

- **Mean**: Average execution time per operation (primary trend metric)
- **Error**: Standard error of measurements (confidence indicator)
- **StdDev**: Standard deviation (consistency indicator)
- **P90/P95**: 90th/95th percentile response times (latency analysis)
- **Allocated**: Memory allocated per operation (memory trend tracking)
- **Ratio**: Performance relative to baseline (regression detection)

### Performance Thresholds

🟢 **Good Performance:**

- Core operations < 50ms average
- Memory allocations stable or decreasing
- Low standard deviation (< 10% of mean)
- **Dashboard shows green trend lines**

🟡 **Acceptable Performance:**

- Core operations 50-100ms average
- Minimal memory growth (< 5% per release)
- Consistent across runs (StdDev < 20% of mean)
- **Dashboard shows yellow warning indicators**

🔴 **Performance Issues:**

- Core operations > 100ms average
- Significant memory increases (> 10% per release)
- High variability between runs (StdDev > 30% of mean)
- **Dashboard shows red alerts and regression markers**

## Best Practices

### Attention Contributors

1. **Use labels wisely:** Only add `performance` label when you suspect performance impact
2. **Test locally first:** Run `dotnet run --configuration Release` in the Benchmarks folder
3. **Compare results:** Look at ratios and trends, not just absolute numbers
4. **Monitor allocations:** Watch for memory allocation increases
5. **Check dashboard:** Review historical context before and after changes
6. **Investigate alerts:** Don't ignore regression warnings in PR comments

## Dashboard Usage Guide

### Viewing Trends

1. **Access Dashboard:** Visit GitHub Pages URL
2. **Select Benchmark:** Each benchmark type has its own chart section
3. **Analyze Trends:**
   - Hover over data points for detailed information
   - Look for patterns and correlations with commits
   - Identify regression points and improvements

### Interpreting Charts

**Time Series Analysis:**

- X-axis: Commit chronology / timestamps
- Y-axis: Performance metric (logarithmic scale)
- Data points: Individual benchmark runs
- Trend lines: Moving averages and regression analysis

**Alert Indicators:**

- 🔴 Red markers: Significant regressions detected
- 🟡 Yellow markers: Minor performance changes
- 🟢 Green markers: Performance improvements

### Regression Investigation

When the dashboard shows performance regressions:

1. **Identify the commit:** Click the regression marker
2. **Review changes:** Examine the code changes in that commit
3. **Correlate impact:** Look at the magnitude of regression
4. **Check consistency:** Verify the regression across multiple runs
5. **Investigate locally:** Reproduce the issue in development environment

## Local Development

### Running Benchmarks Locally

```bash
cd Benchmarks

# Run all benchmarks
dotnet run --configuration Release

# Run specific benchmark
dotnet run --configuration Release -- core
dotnet run --configuration Release -- datasize
dotnet run --configuration Release -- expiration
dotnet run --configuration Release -- concurrency
dotnet run --configuration Release -- bulk
```

### Prerequisites

- .NET 9.0 SDK
- Docker (for PostgreSQL TestContainer)
- At least 4GB RAM available
- x64 platform (recommended)

### Understanding Local vs CI Results

**Local Environment:**

- Your specific hardware
- Fewer variables
- More consistent runs
- Better for development
- **Use for detailed analysis**

**CI Environment:**

- GitHub Actions Ubuntu runner
- Shared/virtualized resources
- Some variability expected
- Good for trend analysis
- **Use for historical tracking**

**Dashboard Integration:**

- Shows both environments when available
- Clearly labels data sources
- Provides context for result interpretation

## Troubleshooting

### Dashboard Issues

**Dashboard not loading:**

```bash
# Check GitHub Pages status
# Verify gh-pages branch exists
# Confirm GitHub Pages is enabled in repository settings
```

**No chart data showing:**

```bash
# Run the setup workflow first
# Execute at least one benchmark
# Wait 5-10 minutes for deployment
# Check browser console for JavaScript errors
```

**Charts showing errors:**

```bash
# Verify JSON data files exist in gh-pages branch
# Check that benchmark JSON output format is correct
# Ensure github-action-benchmark step is running successfully
```

### Common Issues

**Benchmark fails to start:**

```bash
# Check Docker is running
docker ps

# Verify .NET version
dotnet --version

# Ensure BenchmarkDotNet generates JSON output
# Check file paths in workflow configuration
```

**High variability in results:**

- Normal for CI environment
- Focus on trends over absolute values
- Consider multiple runs for critical changes
- Use dashboard trend analysis for patterns

**Regression alerts not firing:**

- Check alert thresholds in workflow files
- Verify baseline data exists for comparison
- Ensure github-action-benchmark step is configured correctly

### Getting Help

1. **Check workflow logs:** GitHub Actions provides detailed execution logs
2. **Review dashboard console:** Browser developer tools for frontend issues
3. **Examine gh-pages branch:** Verify data structure and content
4. **Test locally:** Try reproducing the issue in development environment
5. **Create issue:** Include benchmark results, dashboard screenshots, and environment details

## Performance History

### Viewing Historical Data

- **Live Dashboard:** Interactive charts with full history
- **GitHub Pages:** Automatic updates with each benchmark run
- **Artifacts:** Download from GitHub Actions runs for offline analysis
- **Job Summaries:** View in GitHub Actions UI with dashboard links
- **Release Notes:** Performance summaries with dashboard references
- **Issues:** Performance review issues with historical context

### Exporting Data

```bash
# Data is stored in JSON format in gh-pages branch
# Each benchmark type has its own data file:
# - core-benchmark.json
# - datasize-benchmark.json
# - expiration-benchmark.json
# - concurrency-benchmark.json
# - bulk-benchmark.json

# Clone gh-pages branch for offline analysis
git clone -b gh-pages https://github.com/leonibr/community-extensions-cache-postgres.git dashboard-data
```

### Comparing Performance Across Versions

The dashboard automatically provides:

- **Release markers:** Special indicators for tagged releases
- **Commit correlation:** Direct links to code changes
- **Trend analysis:** Moving averages and regression detection
- **Baseline tracking:** Consistent baseline comparisons

## Advanced Configuration

### Customizing Alert Thresholds

Edit the workflow files to adjust sensitivity:

```yaml
# In benchmarks-scheduled.yml
alert-threshold: '150%'  # Trigger at 50% degradation

# In benchmarks-pr.yml
alert-threshold: '120%'  # Trigger at 20% degradation (more sensitive)
```

### Adding Custom Metrics

To track additional metrics:

1. Modify BenchmarkDotNet configuration in `Program.cs`
2. Update dashboard JavaScript to handle new metrics
3. Adjust chart configurations for optimal visualization

### Dashboard Customization

The dashboard HTML can be customized by:

1. Modifying the setup workflow template
2. Editing the generated `index.html` in gh-pages branch
3. Adding custom CSS/JavaScript for enhanced visualizations
4. Integrating with external monitoring tools

---

**Questions or suggestions?** Open an issue or discussion in the repository.

**Need help with setup?** Run the "Setup Benchmark Dashboard" workflow for guided initialization.

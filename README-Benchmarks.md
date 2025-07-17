# 📊 Performance Benchmarks with Historical Tracking

This repository includes comprehensive performance benchmarking with **historical trend analysis** and **regression detection** - exactly what you need to track performance evolution over time rather than just individual snapshots.

## 🚀 Quick Start

### 1. Initial Setup (One-time)

```bash
# 1. Run the setup workflow in GitHub Actions
Go to: Actions → "Setup Benchmark Dashboard" → Run workflow

# 2. Enable GitHub Pages
Settings → Pages → Deploy from branch "gh-pages"

# 3. Wait 5-10 minutes for deployment
```

### 2. View Your Dashboard

Your live performance dashboard will be at:
**`https://[username].github.io/[repository]/benchmarks/`**

### 3. Start Tracking Performance

```bash
# Automatically runs Monday & Thursday
# Or trigger manually: Actions → "Scheduled Performance Benchmarks"

# For PRs, add 'performance' label or '[perf]' in title
```

## 🎯 What You Get: Historical Analysis Instead of Snapshots

### ❌ Before: Individual Results Only

- Single point-in-time measurements
- No trend analysis
- Manual regression detection
- No baseline comparison

### ✅ After: Complete Historical Tracking

- **📈 Time series charts** showing performance evolution
- **⚡ Automatic regression detection** (alerts at 20-50% degradation)
- **🎯 Baseline comparisons** for every PR
- **🔍 Commit correlation** to identify performance-impacting changes
- **📊 Multi-metric analysis** (time, memory, percentiles)

## 📈 Interactive Dashboard Features

### Historical Trend Charts

- **Performance over time** with commit correlation
- **Multiple metrics** visualization (execution time, memory allocation)
- **Zoom and pan** functionality for detailed analysis
- **Release markers** showing version-to-version changes

### Regression Detection System

- **Automatic alerts** when performance degrades
- **Configurable thresholds**: 20% for PRs, 50% for scheduled runs
- **Commit-level correlation** for root cause analysis
- **Visual indicators** highlighting problem areas

### Comparative Analysis

- **PR vs Main branch** baseline comparisons
- **Release-to-release** performance tracking
- **Cross-benchmark** correlation analysis
- **Long-term trend** identification

## 🔄 Workflow Integration

### 1. Scheduled Monitoring

- **When**: Monday & Thursday at 2 AM UTC + code changes
- **Purpose**: Track performance trends over time
- **Result**: Historical database updates + dashboard refresh

### 2. PR Performance Validation

- **When**: PRs with `performance` label or `[perf]` in title
- **Purpose**: Catch regressions before merge
- **Result**: PR comment with baseline comparison + regression alerts

### 3. Release Performance Validation

- **When**: New releases published
- **Purpose**: Comprehensive validation with full suite
- **Result**: Release notes update + performance review issue

## 📊 Example Dashboard Views

### Core Operations Trends

```
Performance (ms) over Time
    ↑
100 |     ●
 75 |   ●   ●
 50 | ●       ●←── Recent improvement
 25 |           ●
    └─────────────→
    Commits over time
```

### Regression Detection

```
PR #123: [perf] Optimize caching
🔴 Performance Alert: 25% degradation detected
📊 Baseline: 45ms → Current: 56ms
🔗 View trend: [Dashboard Link]
```

## 🛠️ Technical Implementation

### Tools Used

- **[github-action-benchmark](https://github.com/benchmark-action/github-action-benchmark)**: Historical data storage & visualization
- **GitHub Pages**: Free dashboard hosting
- **BenchmarkDotNet**: .NET performance measurement
- **Chart.js**: Interactive charting

### Data Storage

- **Location**: `gh-pages` branch
- **Format**: JSON time series data
- **Retention**:
  - Scheduled runs: 30 days artifacts + permanent dashboard data
  - PR runs: 14 days artifacts
  - Release runs: 1 year artifacts + permanent dashboard data

### Alert Thresholds

- **PR validation**: 20% degradation (sensitive)
- **Scheduled monitoring**: 50% degradation (major issues)
- **Customizable** via workflow configuration

## 📋 Benchmark Types

| Benchmark     | Measures                                  | Runtime    | Dashboard Chart      |
| ------------- | ----------------------------------------- | ---------- | -------------------- |
| `core`        | Basic operations (Get/Set/Delete/Refresh) | ~5-10 min  | Real-time trends     |
| `datasize`    | 1KB to 1MB payload performance            | ~10-15 min | Size impact analysis |
| `expiration`  | Different expiration strategies           | ~10-15 min | Strategy comparison  |
| `concurrency` | 2-16 concurrent operations                | ~15-20 min | Scalability trends   |
| `bulk`        | Batch operations (10-500 items)           | ~15-25 min | Throughput analysis  |

## 🎯 Usage Examples

### For Performance-Sensitive PRs

```bash
# 1. Create PR with performance testing
git checkout -b feature/optimize-caching
# ... make changes ...
git commit -m "[perf] Optimize connection pooling"
git push origin feature/optimize-caching

# 2. GitHub automatically:
#    - Runs core benchmark
#    - Compares vs main branch
#    - Posts results with historical context
#    - Alerts if regression detected
```

### For Release Performance Validation

```bash
# When you create a release, GitHub automatically:
# - Runs full benchmark suite (all 5 types)
# - Updates dashboard with release data
# - Adds performance summary to release notes
# - Creates performance review issue
```

### For Regular Monitoring

```bash
# Automatic scheduled runs every Monday & Thursday
# Dashboard continuously updated with trends
# Regression alerts on significant changes
```

## 🔍 Interpreting Results

### Green Trends 🟢

- Stable or improving performance
- Low variability between runs
- Memory allocations stable

### Yellow Warnings 🟡

- Minor performance changes
- Increased variability
- Worth monitoring

### Red Alerts 🔴

- Significant regressions detected
- High memory growth
- Requires investigation

## 📚 Documentation

- **[Complete Guide](docs/PerformanceTesting.md)**: Detailed setup and usage
- **[Benchmark README](Benchmarks/README.md)**: Technical implementation details
- **[Workflow Files](.github/workflows/)**: GitHub Actions configuration

## 🤝 Contributing

### Adding Performance Testing to PRs

```bash
# Option 1: Add label
Add 'performance' label to your PR

# Option 2: Include in title
Title: "[perf] Your change description"
```

### Investigating Regressions

1. **Check dashboard**: Click regression markers for details
2. **Review commit**: Examine code changes in flagged commit
3. **Test locally**: Reproduce with `dotnet run --configuration Release`
4. **Compare baselines**: Use dashboard historical data

## ❓ FAQ

**Q: Why not run benchmarks on every PR?**  
A: Performance tests are resource-intensive (5-90 minutes). Our opt-in approach prevents CI bottlenecks while providing comprehensive testing when needed.

**Q: How accurate are GitHub Actions runner results?**  
A: Individual results have some variance due to shared infrastructure. The value is in **relative trends and regression detection** rather than absolute performance numbers.

**Q: Can I customize alert thresholds?**  
A: Yes! Edit the `alert-threshold` values in the workflow files. More sensitive for critical performance paths, less sensitive for secondary features.

**Q: How do I view historical data offline?**  
A: Clone the `gh-pages` branch - all data is stored as JSON files you can analyze locally.

---

## 🎉 Result: Complete Historical Performance Tracking

Instead of individual benchmark snapshots, you now have:

✅ **Continuous performance timeline** showing evolution over time  
✅ **Automatic regression detection** with commit-level correlation  
✅ **Interactive dashboard** for trend analysis and investigation  
✅ **Baseline comparisons** for every performance-sensitive change  
✅ **Zero-cost hosting** using GitHub Pages  
✅ **Seamless CI/CD integration** with existing workflows

**Your dashboard**: `https://leonibr.github.io/community-extensions-cache-postgres/benchmarks/`

The horizontal, historical view you requested is now built into every benchmark run! 🚀

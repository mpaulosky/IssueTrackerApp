// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     charts.js
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

window.chartInterop = {
	activeCharts: {},

	renderPieChart: function (canvasId, labels, data, colors) {
		this.destroyChart(canvasId);

		const ctx = document.getElementById(canvasId);
		if (!ctx) {
			console.error(`Canvas element with id '${canvasId}' not found`);
			return;
		}

		const isDarkMode = document.documentElement.classList.contains('dark');
		const textColor = isDarkMode ? '#e5e7eb' : '#1f2937';
		const gridColor = isDarkMode ? '#374151' : '#e5e7eb';

		this.activeCharts[canvasId] = new Chart(ctx, {
			type: 'pie',
			data: {
				labels: labels,
				datasets: [{
					data: data,
					backgroundColor: colors,
					borderColor: isDarkMode ? '#1f2937' : '#ffffff',
					borderWidth: 2
				}]
			},
			options: {
				responsive: true,
				maintainAspectRatio: true,
				plugins: {
					legend: {
						position: 'bottom',
						labels: {
							color: textColor,
							padding: 15,
							font: {
								size: 12
							}
						}
					},
					tooltip: {
						backgroundColor: isDarkMode ? '#374151' : '#ffffff',
						titleColor: textColor,
						bodyColor: textColor,
						borderColor: gridColor,
						borderWidth: 1
					}
				}
			}
		});
	},

	renderBarChart: function (canvasId, labels, data, color) {
		this.destroyChart(canvasId);

		const ctx = document.getElementById(canvasId);
		if (!ctx) {
			console.error(`Canvas element with id '${canvasId}' not found`);
			return;
		}

		const isDarkMode = document.documentElement.classList.contains('dark');
		const textColor = isDarkMode ? '#e5e7eb' : '#1f2937';
		const gridColor = isDarkMode ? '#374151' : '#e5e7eb';

		this.activeCharts[canvasId] = new Chart(ctx, {
			type: 'bar',
			data: {
				labels: labels,
				datasets: [{
					label: 'Issues',
					data: data,
					backgroundColor: color,
					borderColor: color,
					borderWidth: 1
				}]
			},
			options: {
				responsive: true,
				maintainAspectRatio: true,
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							color: textColor,
							stepSize: 1
						},
						grid: {
							color: gridColor
						}
					},
					x: {
						ticks: {
							color: textColor
						},
						grid: {
							display: false
						}
					}
				},
				plugins: {
					legend: {
						display: false
					},
					tooltip: {
						backgroundColor: isDarkMode ? '#374151' : '#ffffff',
						titleColor: textColor,
						bodyColor: textColor,
						borderColor: gridColor,
						borderWidth: 1
					}
				}
			}
		});
	},

	renderLineChart: function (canvasId, labels, datasets) {
		this.destroyChart(canvasId);

		const ctx = document.getElementById(canvasId);
		if (!ctx) {
			console.error(`Canvas element with id '${canvasId}' not found`);
			return;
		}

		const isDarkMode = document.documentElement.classList.contains('dark');
		const textColor = isDarkMode ? '#e5e7eb' : '#1f2937';
		const gridColor = isDarkMode ? '#374151' : '#e5e7eb';

		this.activeCharts[canvasId] = new Chart(ctx, {
			type: 'line',
			data: {
				labels: labels,
				datasets: datasets.map(ds => ({
					label: ds.label,
					data: ds.data,
					borderColor: ds.borderColor,
					backgroundColor: ds.backgroundColor,
					borderWidth: 2,
					fill: false,
					tension: 0.4
				}))
			},
			options: {
				responsive: true,
				maintainAspectRatio: true,
				interaction: {
					mode: 'index',
					intersect: false
				},
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							color: textColor,
							stepSize: 1
						},
						grid: {
							color: gridColor
						}
					},
					x: {
						ticks: {
							color: textColor
						},
						grid: {
							color: gridColor
						}
					}
				},
				plugins: {
					legend: {
						position: 'bottom',
						labels: {
							color: textColor,
							padding: 15,
							font: {
								size: 12
							}
						}
					},
					tooltip: {
						backgroundColor: isDarkMode ? '#374151' : '#ffffff',
						titleColor: textColor,
						bodyColor: textColor,
						borderColor: gridColor,
						borderWidth: 1
					}
				}
			}
		});
	},

	destroyChart: function (canvasId) {
		if (this.activeCharts[canvasId]) {
			this.activeCharts[canvasId].destroy();
			delete this.activeCharts[canvasId];
		}
	},

	destroyAllCharts: function () {
		Object.keys(this.activeCharts).forEach(key => {
			this.activeCharts[key].destroy();
		});
		this.activeCharts = {};
	}
};

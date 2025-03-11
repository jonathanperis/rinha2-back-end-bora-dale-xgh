// docs/js/script.js
// Custom JavaScript for additional functionalities can be added here.
console.log("Rinha de Backend page loaded.");

// Load test reports from the reports folder via the GitHub API.
// Currently, the report links are set to refer to blank pages until real reports are generated.
const reportsList = document.getElementById('reports-list');
if (reportsList) {
  const apiUrl = 'https://api.github.com/repos/jonathanperis/rinha2-back-end-bora-dale-xgh/contents/docs/reports?ref=main';
  
  fetch(apiUrl)
    .then(response => response.json())
    .then(data => {
      // Clear loading message
      reportsList.innerHTML = '';
      
      // Filter HTML files (reports)
      let htmlFiles = data.filter(file => file.name.endsWith('.html'));
      
      // Sort files in descending order by name.
      // Adjust the sorting criteria if your file naming convention is different.
      htmlFiles.sort((a, b) => b.name.localeCompare(a.name));
      
      // Select only the last 5 reports
      htmlFiles = htmlFiles.slice(0, 5);
      
      htmlFiles.forEach(file => {
        // Generating a public URL for GitHub Pages (docs folder is served as the site)
        const publicUrl = `https://jonathanperis.github.io/rinha2-back-end-bora-dale-xgh/reports/${file.name}`;
        const li = document.createElement('li');
        const a = document.createElement('a');
        a.href = publicUrl;
        a.textContent = file.name;
        a.target = "_blank";
        a.className = "text-accent hover:underline";
        li.appendChild(a);
        reportsList.appendChild(li);
      });
      
      // If no HTML files are present, display a message.
      if (!reportsList.children.length) {
        reportsList.innerHTML = '<li>No reports found.</li>';
      }
    })
    .catch(error => {
      console.error('Error fetching reports:', error);
      reportsList.innerHTML = '<li>Error fetching reports.</li>';
    });
}
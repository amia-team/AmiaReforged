pipeline{
    agent any

    stages {
        stage('Build BackupService Docker Image') {
            when {
                changeset "AmiaReforged.BackupService/**"
            }
            steps {
                script {
                    echo 'Changes detected in BackupService, building Docker image...'
                    discordSend description: "Changes detected in BackupService, building Docker image...", footer: "New version detected", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: params.webhookURL

                    dir('AmiaReforged.BackupService') {
                        // Read version from version.txt
                        def version = readFile('version.txt').trim()
                        echo "Building BackupService version ${version}"

                        // Build Docker image with version tag
                        sh "docker build -t amia-backup-service:${version} -t amia-backup-service:latest ."

                        echo "Docker image built successfully: amia-backup-service:${version}"
                    }
                }
            }
        }
        stage('Build AdminPanel Docker Image') {
            when {
                changeset "AmiaReforged.AdminPanel/**"
            }
            steps {
                script {
                    echo 'Changes detected in AdminPanel, building Docker image...'
                    discordSend description: "Changes detected in AdminPanel, building Docker image...", footer: "New version detected", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: params.webhookURL

                    // Read version from version.txt
                    def version = readFile('AmiaReforged.AdminPanel/version.txt').trim()
                    echo "Building AdminPanel version ${version}"

                    // Build Docker image with version tag (Dockerfile expects repo root as context)
                    sh "docker build -f AmiaReforged.AdminPanel/Dockerfile -t amia-admin-panel:${version} -t amia-admin-panel:latest ."

                    echo "Docker image built successfully: amia-admin-panel:${version}"
                }
            }
        }
        stage('Deploy Test') {
            when {
                    expression {
                        return params.DeployTest == true
                    }
            }
            steps {
                echo 'Deploying....'
				sh "chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'chmod +x stop-test.sh'
				sh 'bash stop-test.sh'

                sh 'dotnet publish AmiaReforged.Core --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Core/'
                sh 'dotnet publish AmiaReforged.System --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.System/'
                sh 'dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'dotnet publish AmiaReforged.Races --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.Races/'
                sh 'dotnet publish AmiaReforged.DMS --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.DMS/'
                sh 'dotnet publish AmiaReforged.PwEngine --output /home/amia/amia_server/test_server/anvil/Plugins/AmiaReforged.PwEngine/'

                script {
                    if (params.resources_dest_test?.trim()) {
                        sh "rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_test}/"
                    } else {
                        echo 'WARNING: resources_dest_test parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'chmod +x start-test.sh'
				sh 'bash start-test.sh'
            }
        }
		stage('Deploy Live') {
            when {
                    expression {
                        return params.DeployLive == true
                    }
            }
            steps {
                echo 'Deploying....'
				sh "chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'chmod +x stop-live.sh'
				sh 'bash stop-live.sh'

                sh 'dotnet publish AmiaReforged.Core --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Core/'
                sh 'dotnet publish AmiaReforged.System --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.System/'
                sh 'dotnet publish AmiaReforged.Classes --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Classes/'
                sh 'dotnet publish AmiaReforged.Races --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.Races/'
                sh 'dotnet publish AmiaReforged.DMS --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.DMS/'
                sh 'dotnet publish AmiaReforged.PwEngine --output /home/amia/amia_server/server/anvil/Plugins/AmiaReforged.PwEngine/'

                script {
                    if (params.resources_dest_prod?.trim()) {
                        sh "rsync -av --delete AmiaReforged.PwEngine/Resources/WorldEngine ${params.resources_dest_prod}/"
                    } else {
                        echo 'WARNING: resources_dest_prod parameter is empty. Skipping WorldEngine resource deployment.'
                    }
                }

				sh "chown -R jenkins.jenkins /var/lib/jenkins/workspace/"
				sh 'chmod +x start-live.sh'
				sh 'bash start-live.sh'
            }
        }
    }
    post {
        always {
            discordSend description: "Builder for plugin AmiaReforged finished.", footer: "Build results for AmiaReforged", link: env.BUILD_URL, result: currentBuild.currentResult, title: JOB_NAME, webhookURL: params.webhookURL
        }
        success {
            echo 'Build success'
        }
    }
}
